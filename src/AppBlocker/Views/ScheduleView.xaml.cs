using System.Windows.Controls;
using AppBlocker.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AppBlocker.Views;

public partial class ScheduleView : UserControl
{
    public ScheduleView()
    {
        InitializeComponent();
        var vm = App.Services.GetRequiredService<ScheduleViewModel>();
        DataContext = vm;
        Loaded += async (_, _) => await vm.LoadAsync();
    }
}
