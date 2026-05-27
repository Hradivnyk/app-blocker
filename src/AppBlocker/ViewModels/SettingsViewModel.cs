using CommunityToolkit.Mvvm.ComponentModel;

namespace AppBlocker.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _startWithWindows;
}
