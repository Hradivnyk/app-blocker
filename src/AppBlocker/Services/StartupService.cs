using Microsoft.Extensions.Logging;

namespace AppBlocker.Services;

public class StartupService : IStartupService
{
    private readonly ILogger<StartupService> _logger;

    public StartupService(ILogger<StartupService> logger)
    {
        _logger = logger;
    }

    public bool IsStartupEnabled()
    {
        _logger.LogDebug("IsStartupEnabled (stub).");
        return false;
    }

    public Task EnableStartupAsync()
    {
        _logger.LogInformation("EnableStartup (stub).");
        return Task.CompletedTask;
    }

    public Task DisableStartupAsync()
    {
        _logger.LogInformation("DisableStartup (stub).");
        return Task.CompletedTask;
    }
}
