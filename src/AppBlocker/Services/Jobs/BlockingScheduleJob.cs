using Microsoft.Extensions.Logging;
using Quartz;

namespace AppBlocker.Services.Jobs;

[DisallowConcurrentExecution]
public class BlockingScheduleJob : IJob
{
    public const string BlockedAppIdKey = "BlockedAppId";
    public const string EnablesBlockingKey = "EnablesBlocking";

    private readonly IBlockerService _blockerService;
    private readonly ILogger<BlockingScheduleJob> _logger;

    public BlockingScheduleJob(IBlockerService blockerService, ILogger<BlockingScheduleJob> logger)
    {
        _blockerService = blockerService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var data = context.MergedJobDataMap;
        int appId = data.GetInt(BlockedAppIdKey);
        bool enables = data.GetBoolean(EnablesBlockingKey);

        _logger.LogInformation(
            "BlockingScheduleJob fired: AppId={AppId}, EnablesBlocking={Enables}",
            appId, enables);

        if (enables)
            await _blockerService.EnableBlockingForAppAsync(appId, context.CancellationToken);
        else
            await _blockerService.DisableBlockingForAppAsync(appId, context.CancellationToken);
    }
}
