namespace AppBlocker.Services;

public interface IStartupService
{
    Task<bool> IsStartupEnabledAsync();
    Task EnableStartupAsync();
    Task DisableStartupAsync();
}
