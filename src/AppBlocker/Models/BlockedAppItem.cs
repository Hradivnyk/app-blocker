namespace AppBlocker.Models;

public class BlockedAppItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
}
