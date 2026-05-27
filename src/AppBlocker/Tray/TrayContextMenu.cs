using System.Windows.Controls;

namespace AppBlocker.Tray;

internal static class TrayContextMenu
{
    internal static ContextMenu Build(Action openWindow, Action exitApp)
    {
        var openItem = new MenuItem { Header = "Відкрити AppBlocker" };
        openItem.Click += (_, _) => openWindow();

        var exitItem = new MenuItem { Header = "Вийти" };
        exitItem.Click += (_, _) => exitApp();

        var menu = new ContextMenu();
        menu.Items.Add(openItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);
        return menu;
    }
}
