using Microsoft.Extensions.Logging;

namespace AppBlocker.Tray;

/// <summary>
/// Manages the system tray icon and context menu.
/// Full implementation in Step 2.
/// </summary>
public class TrayIconManager : IDisposable
{
    private readonly ILogger<TrayIconManager> _logger;
    private bool _disposed;

    public TrayIconManager(ILogger<TrayIconManager> logger)
    {
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.LogInformation("TrayIconManager initialized (stub).");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
