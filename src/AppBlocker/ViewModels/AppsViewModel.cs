using CommunityToolkit.Mvvm.ComponentModel;

namespace AppBlocker.ViewModels;

public partial class AppsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "No apps configured.";
}
