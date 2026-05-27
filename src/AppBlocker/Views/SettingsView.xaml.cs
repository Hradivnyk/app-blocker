using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using AppBlocker.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AppBlocker.Views;

public partial class SettingsView : UserControl
{
    private static readonly Regex DigitsOnly = new(@"[^0-9]");

    public SettingsView()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SettingsViewModel>();
    }

    private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = DigitsOnly.IsMatch(e.Text);
    }
}
