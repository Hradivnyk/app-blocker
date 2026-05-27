using CommunityToolkit.Mvvm.ComponentModel;

namespace AppBlocker.ViewModels;

public partial class ScheduleViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "No schedules configured.";
}
