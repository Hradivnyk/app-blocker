using Microsoft.Extensions.Logging;

namespace AppBlocker.Services;

public class SchedulerService : ISchedulerService
{
    private readonly ILogger<SchedulerService> _logger;

    public SchedulerService(ILogger<SchedulerService> logger)
    {
        _logger = logger;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SchedulerService initialized (stub).");
        return Task.CompletedTask;
    }

    public Task ScheduleAppAsync(int blockedAppId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Schedule app {Id} (stub).", blockedAppId);
        return Task.CompletedTask;
    }

    public Task UnscheduleAppAsync(int blockedAppId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Unschedule app {Id} (stub).", blockedAppId);
        return Task.CompletedTask;
    }

    public Task PauseAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PauseAll (stub).");
        return Task.CompletedTask;
    }

    public Task ResumeAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ResumeAll (stub).");
        return Task.CompletedTask;
    }
}
