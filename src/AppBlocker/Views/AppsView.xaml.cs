using System.Windows.Controls;
using AppBlocker.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AppBlocker.Views;

public partial class AppsView : UserControl
{
    public AppsView()
    {
        InitializeComponent();
        var vm = App.Services.GetRequiredService<AppsViewModel>();
        DataContext = vm;
        Loaded += async (_, _) => await vm.LoadAsync();
    }
}
