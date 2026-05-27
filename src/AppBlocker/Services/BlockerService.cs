using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows;
using AppBlocker.Data;
using AppBlocker.Models;
using AppBlocker.ViewModels;
using AppBlocker.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AppBlocker.Services;

public class BlockerService : IBlockerService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BlockerService> _logger;
    private readonly ProcessWatcherService _processWatcher;

    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(2);

    private System.Threading.Timer? _pollingTimer;
    private readonly SemaphoreSlim _pollLock = new(1, 1);
    private readonly ConcurrentDictionary<int, DateTime> _temporaryUnblocks = new();

    public BlockerService(
        IServiceScopeFactory scopeFactory,
        ILogger<BlockerService> logger,
        ProcessWatcherService processWatcher)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _processWatcher = processWatcher;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await RefreshWatchedNamesAsync(cancellationToken);

        _processWatcher.ProcessStarted = OnWmiProcessStarted;
        bool wmiOk = await _processWatcher.StartAsync(cancellationToken);

        // Keep polling timer running as a safety net regardless of WMI availability
        _pollingTimer = new System.Threading.Timer(OnPollTick, null, TimeSpan.Zero, PollingInterval);

        _logger.LogInformation("BlockerService started ({Mode}).",
            wmiOk ? "WMI + polling safety net" : "polling only");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _pollingTimer?.Dispose();
        _pollingTimer = null;

        _processWatcher.ProcessStarted = null;
        await _processWatcher.StopAsync(cancellationToken);

        _logger.LogInformation("BlockerService stopped.");
    }

    public async Task EnableBlockingForAppAsync(int blockedAppId, CancellationToken cancellationToken = default)
    {
        _temporaryUnblocks.TryRemove(blockedAppId, out _);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var app = await db.BlockedApps.FindAsync(new object[] { blockedAppId }, cancellationToken);
        if (app is null) return;

        app.IsEnabled = true;
        await db.SaveChangesAsync(cancellationToken);
        _processWatcher.Watch(app.Name);
        _logger.LogInformation("Blocking enabled for '{Name}' (Id={Id}).", app.Name, app.Id);
    }

    public async Task DisableBlockingForAppAsync(int blockedAppId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var app = await db.BlockedApps.FindAsync(new object[] { blockedAppId }, cancellationToken);
        if (app is null) return;

        app.IsEnabled = false;
        await db.SaveChangesAsync(cancellationToken);
        _processWatcher.Unwatch(app.Name);
        _logger.LogInformation("Blocking disabled for '{Name}' (Id={Id}).", app.Name, app.Id);
    }

    public Task TemporarilyUnblockAsync(int blockedAppId, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var expiry = DateTime.UtcNow + duration;
        _temporaryUnblocks[blockedAppId] = expiry;
        _logger.LogInformation("App {Id} temporarily unblocked for {Duration} (expires {Expiry:HH:mm:ss} UTC).",
            blockedAppId, duration, expiry);
        return Task.CompletedTask;
    }

    // WMI event path — called from ProcessWatcherService on a ThreadPool thread
    private void OnWmiProcessStarted(string processName, int pid)
        => _ = HandleProcessStartAsync(processName);

    private async Task HandleProcessStartAsync(string processName)
    {
        List<BlockedApp> blockedApps;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            blockedApps = await db.BlockedApps
                .Where(a => a.IsEnabled && a.Name == processName)
                .ToListAsync();
        }

        foreach (var app in blockedApps)
        {
            if (_temporaryUnblocks.TryGetValue(app.Id, out var expiry) && DateTime.UtcNow < expiry)
                continue;

            KillBlockedProcesses(app.Name);
            break;
        }
    }

    private async Task RefreshWatchedNamesAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var apps = await db.BlockedApps.Where(a => a.IsEnabled).ToListAsync(ct);
        foreach (var app in apps)
            _processWatcher.Watch(app.Name);
    }

    // Polling safety net path
    private async void OnPollTick(object? state)
    {
        if (!await _pollLock.WaitAsync(0)) return;
        try
        {
            await PollAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in BlockerService poll tick.");
        }
        finally
        {
            _pollLock.Release();
        }
    }

    private async Task PollAsync()
    {
        List<BlockedApp> blockedApps;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            blockedApps = await db.BlockedApps.Where(a => a.IsEnabled).ToListAsync();
        }

        foreach (var app in blockedApps)
        {
            if (_temporaryUnblocks.TryGetValue(app.Id, out var expiry))
            {
                if (DateTime.UtcNow < expiry) continue;
                _temporaryUnblocks.TryRemove(app.Id, out _);
                _logger.LogInformation("Temporary unblock expired for '{Name}' (Id={Id}).", app.Name, app.Id);
            }

            KillBlockedProcesses(app.Name);
        }
    }

    private void KillBlockedProcesses(string appName)
    {
        int currentSessionId = Process.GetCurrentProcess().SessionId;

        Process[] processes;
        try
        {
            processes = Process.GetProcessesByName(appName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate processes for '{AppName}'.", appName);
            return;
        }

        foreach (var process in processes)
        {
            try
            {
                if (process.SessionId != currentSessionId)
                {
                    process.Dispose();
                    continue;
                }

                int pid = process.Id;
                process.Kill();

                bool exited = process.WaitForExit(1000);
                if (!exited || !process.HasExited)
                    _logger.LogWarning("Process '{AppName}' (PID {Pid}) may still be running after kill.", appName, pid);
                else
                    _logger.LogInformation("Killed process '{AppName}' (PID {Pid}).", appName, pid);

                ShowBlockedNotification(appName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error killing '{AppName}' (PID {Pid}).", appName, process.Id);
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private static void ShowBlockedNotification(string appName)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var vm = new BlockedNotificationViewModel(appName);
            var window = new BlockedNotificationWindow(vm);
            window.Show();
        });
    }
}
