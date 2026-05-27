using AppBlocker.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace AppBlocker.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IStartupService _startupService;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _isBusy;

    public SettingsViewModel(IStartupService startupService, ILogger<SettingsViewModel> logger)
    {
        _startupService = startupService;
        _logger = logger;
        _ = LoadStartupStateAsync();
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        _ = ApplyStartupSettingAsync(value);
    }

    private async Task LoadStartupStateAsync()
    {
        IsBusy = true;
        try
        {
            StartWithWindows = await _startupService.IsStartupEnabledAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load startup state.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ApplyStartupSettingAsync(bool enable)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            if (enable)
                await _startupService.EnableStartupAsync();
            else
                await _startupService.DisableStartupAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply startup setting.");
            IsBusy = true;
            StartWithWindows = !enable;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
