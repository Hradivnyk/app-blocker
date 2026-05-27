namespace AppBlocker.Models;

public class AppSettings
{
    public bool ShowBlockNotification { get; set; } = true;
    public int NotificationAutoCloseSeconds { get; set; } = 10;
    public int DefaultTemporaryUnblockMinutes { get; set; } = 5;
}
