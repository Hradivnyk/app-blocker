namespace AppBlocker.Services;

public interface ISchedulerService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task ScheduleAppAsync(int blockedAppId, CancellationToken cancellationToken = default);
    Task UnscheduleAppAsync(int blockedAppId, CancellationToken cancellationToken = default);
    Task PauseAllAsync(CancellationToken cancellationToken = default);
    Task ResumeAllAsync(CancellationToken cancellationToken = default);
}
