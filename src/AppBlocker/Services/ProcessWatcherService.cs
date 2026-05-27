using Microsoft.Extensions.Logging;

namespace AppBlocker.Services;

/// <summary>
/// WMI Win32_ProcessStartTrace subscription with polling fallback.
/// Full implementation in Step 9.
/// </summary>
public class ProcessWatcherService : IDisposable
{
    private readonly ILogger<ProcessWatcherService> _logger;
    private bool _disposed;

    public ProcessWatcherService(ILogger<ProcessWatcherService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ProcessWatcherService started (stub).");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ProcessWatcherService stopped (stub).");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
