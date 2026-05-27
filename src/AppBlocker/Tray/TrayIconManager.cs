using System.Runtime.InteropServices;
using System.Windows;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.Extensions.Logging;

namespace AppBlocker.Tray;

public sealed class TrayIconManager : IDisposable
{
    private readonly ILogger<TrayIconManager> _logger;
    private readonly Action _openWindow;
    private readonly Action _exitApp;

    private TaskbarIcon? _taskbarIcon;
    private System.Drawing.Icon? _icon;
    private bool _disposed;

    public TrayIconManager(ILogger<TrayIconManager> logger, Action openWindow, Action exitApp)
    {
        _logger = logger;
        _openWindow = openWindow;
        _exitApp = exitApp;
    }

    /// <summary>
    /// Creates and registers the native tray icon. Must be called on the WPF UI thread.
    /// </summary>
    public void Initialize()
    {
        _icon = CreateAppIcon();

        _taskbarIcon = new TaskbarIcon
        {
            Icon = _icon,
            ToolTipText = "AppBlocker",
            MenuActivation = PopupActivationMode.RightClick,
            ContextMenu = TrayContextMenu.Build(_openWindow, _exitApp),
        };

        _taskbarIcon.TrayMouseDoubleClick += OnTrayDoubleClick;

        // ForceCreate is required for code-only initialization (not via XAML),
        // so the native Win32 icon gets registered in the system tray.
        _taskbarIcon.ForceCreate(enablesEfficiencyMode: false);

        _logger.LogInformation("TrayIconManager initialized.");
    }

    private void OnTrayDoubleClick(object sender, RoutedEventArgs e)
    {
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow is { IsVisible: true })
        {
            _logger.LogDebug("Tray double-click: hiding main window.");
            mainWindow.Hide();
        }
        else
        {
            _logger.LogDebug("Tray double-click: showing main window.");
            _openWindow();
        }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private static System.Drawing.Icon CreateAppIcon()
    {
        const int size = 32;
        using var bmp = new System.Drawing.Bitmap(size, size);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(System.Drawing.Color.Transparent);

        using var bgBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(26, 83, 255));
        g.FillRectangle(bgBrush, 2, 2, size - 4, size - 4);

        using var font = new System.Drawing.Font("Segoe UI", 18, System.Drawing.FontStyle.Bold);
        var textRect = new System.Drawing.RectangleF(0, 2, size, size);
        var format = new System.Drawing.StringFormat
        {
            Alignment = System.Drawing.StringAlignment.Center,
            LineAlignment = System.Drawing.StringAlignment.Center,
        };
        g.DrawString("B", font, System.Drawing.Brushes.White, textRect, format);

        var hIcon = bmp.GetHicon();
        try
        {
            using var temp = System.Drawing.Icon.FromHandle(hIcon);
            return (System.Drawing.Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_taskbarIcon is not null)
        {
            _taskbarIcon.TrayMouseDoubleClick -= OnTrayDoubleClick;
            _taskbarIcon.Dispose();
            _taskbarIcon = null;
        }

        _icon?.Dispose();
        _logger.LogInformation("TrayIconManager disposed.");
    }
}
