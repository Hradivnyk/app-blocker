using CommunityToolkit.Mvvm.ComponentModel;

namespace AppBlocker.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "AppBlocker";
}
