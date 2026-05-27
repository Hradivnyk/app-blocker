using System.Windows;
using AppBlocker.ViewModels;

namespace AppBlocker.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Per spec: closing hides, not destroys. True exit via tray "Exit" only.
        Closing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
        };
    }
}
