namespace AppBlocker.Models;

/// <summary>
/// Quartz 6-field cron format: seconds minutes hours day-of-month month day-of-week.
/// Example: "0 0 9 ? * MON-FRI"
/// </summary>
public class BlockSchedule
{
    public int Id { get; set; }

    public int BlockedAppId { get; set; }

    public string CronExpression { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public BlockedApp? BlockedApp { get; set; }
}
