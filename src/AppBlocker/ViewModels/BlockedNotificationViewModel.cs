using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppBlocker.ViewModels;

public partial class BlockedNotificationViewModel : ObservableObject
{
    private readonly DispatcherTimer _timer;
    private int _remainingSeconds = 10;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private int _countdown = 10;

    public event EventHandler? CloseRequested;

    public BlockedNotificationViewModel(string appName)
    {
        Message = $"{appName} заблоковано";

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _remainingSeconds--;
        Countdown = _remainingSeconds;

        if (_remainingSeconds <= 0)
        {
            _timer.Stop();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void Close()
    {
        _timer.Stop();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Cleanup() => _timer.Stop();
}
