using System.Windows.Controls;
using AppBlocker.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AppBlocker.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SettingsViewModel>();
    }
}
