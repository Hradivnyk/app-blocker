namespace AppBlocker.Models;

public class ScheduleItem
{
    public int Id { get; init; }
    public int BlockedAppId { get; init; }
    public string CronExpression { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public bool EnablesBlocking { get; init; }
    public string Label { get; init; } = string.Empty;
}
