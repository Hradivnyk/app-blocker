namespace AppBlocker.Models;

public class BlockedApp
{
    public int Id { get; set; }

    /// <summary>Process name without .exe extension (e.g. "chrome").</summary>
    public string Name { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public ICollection<BlockSchedule> Schedules { get; set; } = new List<BlockSchedule>();
}
