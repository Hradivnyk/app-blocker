using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace AppBlocker.Services;

public class StartupService : IStartupService
{
    private const string RegistryKeyName = "AppBlocker";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly ILogger<StartupService> _logger;

    public StartupService(ILogger<StartupService> logger)
    {
        _logger = logger;
    }

    public Task<bool> IsStartupEnabledAsync()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            if (key is null)
                return Task.FromResult(false);

            var value = key.GetValue(RegistryKeyName) as string;
            if (string.IsNullOrEmpty(value))
                return Task.FromResult(false);

            var exePath = Environment.ProcessPath ?? string.Empty;
            var expected = BuildRegistryValue(exePath);

            bool matches = string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
            _logger.LogDebug("IsStartupEnabled: {Matches} (stored={Value})", matches, value);
            return Task.FromResult(matches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read startup registry key.");
            return Task.FromResult(false);
        }
    }

    public Task EnableStartupAsync()
    {
        try
        {
            var exePath = Environment.ProcessPath ?? string.Empty;
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key is null)
            {
                _logger.LogError("Cannot open registry Run key for writing.");
                return Task.CompletedTask;
            }

            key.SetValue(RegistryKeyName, BuildRegistryValue(exePath));
            _logger.LogInformation("Autostart enabled for '{ExePath}'.", exePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable autostart.");
        }

        return Task.CompletedTask;
    }

    public Task DisableStartupAsync()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(RegistryKeyName, throwOnMissingValue: false);
            _logger.LogInformation("Autostart disabled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable autostart.");
        }

        return Task.CompletedTask;
    }

    private static string BuildRegistryValue(string exePath) =>
        $"\"{exePath}\" --minimized";
}
