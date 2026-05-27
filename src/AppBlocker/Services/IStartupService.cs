namespace AppBlocker.Services;

public interface IStartupService
{
    bool IsStartupEnabled();
    Task EnableStartupAsync();
    Task DisableStartupAsync();
}
