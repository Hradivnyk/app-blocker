using AppBlocker.Data;
using AppBlocker.Models;
using AppBlocker.Services.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace AppBlocker.Services;

public class SchedulerService : ISchedulerService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SchedulerService> _logger;

    private IScheduler? _scheduler;

    public SchedulerService(
        ISchedulerFactory schedulerFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<SchedulerService> logger)
    {
        _schedulerFactory = schedulerFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var schedules = await db.BlockSchedules
            .Include(s => s.BlockedApp)
            .Where(s => s.IsEnabled)
            .ToListAsync(cancellationToken);

        int count = 0;
        foreach (var schedule in schedules)
        {
            if (schedule.BlockedApp?.IsEnabled != true) continue;
            await ScheduleOneAsync(schedule, cancellationToken);
            count++;
        }

        _logger.LogInformation("SchedulerService initialized. Restored {Count} schedules.", count);
    }

    public async Task ScheduleAppAsync(int blockedAppId, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        await DeleteGroupJobsAsync(blockedAppId, cancellationToken);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var schedules = await db.BlockSchedules
            .Where(s => s.BlockedAppId == blockedAppId && s.IsEnabled)
            .ToListAsync(cancellationToken);

        foreach (var schedule in schedules)
            await ScheduleOneAsync(schedule, cancellationToken);

        _logger.LogInformation(
            "Scheduled {Count} jobs for app {AppId}.", schedules.Count, blockedAppId);
    }

    public async Task UnscheduleAppAsync(int blockedAppId, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        await DeleteGroupJobsAsync(blockedAppId, cancellationToken);
        _logger.LogInformation("Unscheduled all jobs for app {AppId}.", blockedAppId);
    }

    public Task PauseAllAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        _logger.LogInformation("Pausing all schedules.");
        return _scheduler!.PauseAll(cancellationToken);
    }

    public Task ResumeAllAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        _logger.LogInformation("Resuming all schedules.");
        return _scheduler!.ResumeAll(cancellationToken);
    }

    private async Task ScheduleOneAsync(BlockSchedule schedule, CancellationToken ct)
    {
        var jobKey = new JobKey($"schedule-{schedule.Id}", $"app-{schedule.BlockedAppId}");
        var triggerKey = new TriggerKey($"trigger-{schedule.Id}", $"app-{schedule.BlockedAppId}");

        IJobDetail job = JobBuilder.Create<BlockingScheduleJob>()
            .WithIdentity(jobKey)
            .UsingJobData(BlockingScheduleJob.BlockedAppIdKey, schedule.BlockedAppId)
            .UsingJobData(BlockingScheduleJob.EnablesBlockingKey, schedule.EnablesBlocking)
            .Build();

        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .WithCronSchedule(schedule.CronExpression, x => x
                .InTimeZone(TimeZoneInfo.Local)
                .WithMisfireHandlingInstructionDoNothing())
            .Build();

        await _scheduler!.ScheduleJob(job, trigger, ct);

        _logger.LogDebug(
            "Scheduled job {JobKey} with cron '{Cron}', EnablesBlocking={Enables}.",
            jobKey, schedule.CronExpression, schedule.EnablesBlocking);
    }

    private async Task DeleteGroupJobsAsync(int blockedAppId, CancellationToken ct)
    {
        var matcher = Quartz.Impl.Matchers.GroupMatcher<JobKey>.GroupEquals($"app-{blockedAppId}");
        var keys = await _scheduler!.GetJobKeys(matcher, ct);
        if (keys.Count > 0)
            await _scheduler!.DeleteJobs(keys.ToList(), ct);
    }

    private void EnsureInitialized()
    {
        if (_scheduler is null)
            throw new InvalidOperationException("SchedulerService.InitializeAsync must be called before use.");
    }
}
