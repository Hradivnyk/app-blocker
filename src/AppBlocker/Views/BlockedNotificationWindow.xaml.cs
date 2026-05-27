using System.Windows;
using AppBlocker.ViewModels;

namespace AppBlocker.Views;

public partial class BlockedNotificationWindow : Window
{
    private readonly BlockedNotificationViewModel _vm;

    public BlockedNotificationWindow(BlockedNotificationViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        vm.CloseRequested += (_, _) => Close();

        Closing += (_, _) => _vm.Cleanup();

        PositionBottomRight();
    }

    private void PositionBottomRight()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 12;
        Top = area.Bottom - Height - 12;
    }
}
