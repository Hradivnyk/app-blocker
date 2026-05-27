using System.Diagnostics;
using System.IO;
using System.Reflection;
using AppBlocker.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AppBlocker.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IStartupService _startupService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsViewModel> _logger;

    private bool _suppressSave;

    [ObservableProperty]
    private bool _isBusy;

    // General
    [ObservableProperty]
    private bool _startWithWindows;

    // Notifications
    [ObservableProperty]
    private bool _showBlockNotification;

    [ObservableProperty]
    private int _notificationAutoCloseSeconds;

    // Blocking
    [ObservableProperty]
    private int _defaultTemporaryUnblockMinutes;

    // About
    [ObservableProperty]
    private string _appVersion = string.Empty;

    public SettingsViewModel(
        IStartupService startupService,
        ISettingsService settingsService,
        ILogger<SettingsViewModel> logger)
    {
        _startupService = startupService;
        _settingsService = settingsService;
        _logger = logger;

        AppVersion = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
            ?? "1.0.0";

        LoadFromSettings();
        _ = LoadStartupStateAsync();
    }

    private void LoadFromSettings()
    {
        _suppressSave = true;
        var s = _settingsService.Current;
        ShowBlockNotification = s.ShowBlockNotification;
        NotificationAutoCloseSeconds = s.NotificationAutoCloseSeconds;
        DefaultTemporaryUnblockMinutes = s.DefaultTemporaryUnblockMinutes;
        _suppressSave = false;
    }

    partial void OnStartWithWindowsChanged(bool value)
        => _ = ApplyStartupSettingAsync(value);

    partial void OnShowBlockNotificationChanged(bool value)
        => PersistSettings();

    partial void OnNotificationAutoCloseSecondsChanged(int value)
        => PersistSettings();

    partial void OnDefaultTemporaryUnblockMinutesChanged(int value)
        => PersistSettings();

    private void PersistSettings()
    {
        if (_suppressSave)
            return;

        var s = _settingsService.Current;
        s.ShowBlockNotification = ShowBlockNotification;
        s.NotificationAutoCloseSeconds = Math.Clamp(NotificationAutoCloseSeconds, 5, 30);
        s.DefaultTemporaryUnblockMinutes = Math.Clamp(DefaultTemporaryUnblockMinutes, 1, 15);
        _ = _settingsService.SaveAsync();
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
            _suppressSave = true;
            StartWithWindows = !enable;
            _suppressSave = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AppBlocker", "Logs");
        Directory.CreateDirectory(path);
        Process.Start("explorer.exe", path);
    }

    [RelayCommand]
    private void OpenDataFolder()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AppBlocker");
        Directory.CreateDirectory(path);
        Process.Start("explorer.exe", path);
    }
}
