namespace AppBlocker.Services;

public interface IBlockerService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task EnableBlockingForAppAsync(int blockedAppId, CancellationToken cancellationToken = default);
    Task DisableBlockingForAppAsync(int blockedAppId, CancellationToken cancellationToken = default);
    Task TemporarilyUnblockAsync(int blockedAppId, TimeSpan duration, CancellationToken cancellationToken = default);
}
