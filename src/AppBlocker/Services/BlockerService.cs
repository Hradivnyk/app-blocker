using Microsoft.Extensions.Logging;

namespace AppBlocker.Services;

public class BlockerService : IBlockerService
{
    private readonly ILogger<BlockerService> _logger;

    public BlockerService(ILogger<BlockerService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("BlockerService started (stub).");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("BlockerService stopped (stub).");
        return Task.CompletedTask;
    }

    public Task EnableBlockingForAppAsync(int blockedAppId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("EnableBlocking app {Id} (stub).", blockedAppId);
        return Task.CompletedTask;
    }

    public Task DisableBlockingForAppAsync(int blockedAppId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DisableBlocking app {Id} (stub).", blockedAppId);
        return Task.CompletedTask;
    }

    public Task TemporarilyUnblockAsync(int blockedAppId, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("TemporarilyUnblock app {Id} for {Duration} (stub).", blockedAppId, duration);
        return Task.CompletedTask;
    }
}
