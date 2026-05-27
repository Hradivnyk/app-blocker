using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Management;
using Microsoft.Extensions.Logging;

namespace AppBlocker.Services;

public class ProcessWatcherService : IDisposable
{
    private readonly ILogger<ProcessWatcherService> _logger;

    // Wired by BlockerService.StartAsync to avoid circular DI dependency
    public Action<string, int>? ProcessStarted { get; set; }

    private readonly ConcurrentDictionary<string, byte> _watchedNames
        = new(StringComparer.OrdinalIgnoreCase);

    // Fallback path: suppress duplicate callbacks for the same PID across poll ticks
    private readonly ConcurrentDictionary<int, byte> _seenPids = new();

    private ManagementEventWatcher? _watcher;
    private System.Threading.Timer? _fallbackTimer;
    private bool _disposed;

    private static readonly TimeSpan FallbackInterval = TimeSpan.FromSeconds(2);

    public ProcessWatcherService(ILogger<ProcessWatcherService> logger)
    {
        _logger = logger;
    }

    public Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        if (TryStartWmi())
            return Task.FromResult(true);

        StartFallbackPolling();
        return Task.FromResult(false);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        StopWatcher();
        _fallbackTimer?.Dispose();
        _fallbackTimer = null;
        _logger.LogInformation("ProcessWatcherService stopped.");
        return Task.CompletedTask;
    }

    public void Watch(string processName)
    {
        _watchedNames.TryAdd(processName, 0);
        _seenPids.Clear();
    }

    public void Unwatch(string processName)
    {
        _watchedNames.TryRemove(processName, out _);
    }

    private bool TryStartWmi()
    {
        try
        {
            var query = new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace");
            _watcher = new ManagementEventWatcher(query);
            _watcher.EventArrived += OnWmiEventArrived;
            _watcher.Start();
            _logger.LogInformation("ProcessWatcherService started (WMI Win32_ProcessStartTrace).");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WMI unavailable; polling fallback will be used.");
            _watcher?.Dispose();
            _watcher = null;
            return false;
        }
    }

    private void OnWmiEventArrived(object sender, EventArrivedEventArgs e)
    {
        try
        {
            string? rawName = e.NewEvent["ProcessName"] as string;
            if (rawName is null) return;

            string nameNoExt = Path.GetFileNameWithoutExtension(rawName);
            if (!_watchedNames.ContainsKey(nameNoExt)) return;

            int pid = (int)(uint)e.NewEvent["ProcessID"];
            int sessionId = (int)(uint)e.NewEvent["SessionID"];

            if (sessionId == Process.GetCurrentProcess().SessionId)
                ProcessStarted?.Invoke(nameNoExt, pid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WMI Win32_ProcessStartTrace event.");
        }
    }

    private void StartFallbackPolling()
    {
        _fallbackTimer = new System.Threading.Timer(OnFallbackTick, null, TimeSpan.Zero, FallbackInterval);
        _logger.LogInformation("ProcessWatcherService started (polling fallback every {s}s).",
            FallbackInterval.TotalSeconds);
    }

    private void OnFallbackTick(object? state)
    {
        int currentSession = Process.GetCurrentProcess().SessionId;

        foreach (string name in _watchedNames.Keys)
        {
            Process[] procs;
            try { procs = Process.GetProcessesByName(name); }
            catch { continue; }

            foreach (var proc in procs)
            {
                try
                {
                    if (proc.SessionId == currentSession && _seenPids.TryAdd(proc.Id, 0))
                        ProcessStarted?.Invoke(name, proc.Id);
                }
                finally { proc.Dispose(); }
            }
        }

        // Prune PIDs that are no longer alive so the dictionary doesn't grow indefinitely
        foreach (int pid in _seenPids.Keys)
        {
            try { using var p = Process.GetProcessById(pid); }
            catch (ArgumentException) { _seenPids.TryRemove(pid, out _); }
        }
    }

    private void StopWatcher()
    {
        if (_watcher is null) return;
        try { _watcher.Stop(); } catch { }
        _watcher.Dispose();
        _watcher = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopWatcher();
        _fallbackTimer?.Dispose();
    }
}
