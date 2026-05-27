using System.Collections.ObjectModel;
using AppBlocker.Data;
using AppBlocker.Models;
using AppBlocker.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AppBlocker.ViewModels;

public partial class ScheduleViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISchedulerService _schedulerService;
    private readonly ILogger<ScheduleViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<BlockedAppItem> _apps = new();

    [ObservableProperty]
    private BlockedAppItem? _selectedApp;

    [ObservableProperty]
    private ObservableCollection<ScheduleItem> _schedules = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // Add-form fields
    [ObservableProperty] private int _startHour = 9;
    [ObservableProperty] private int _startMinute = 0;
    [ObservableProperty] private int _endHour = 18;
    [ObservableProperty] private int _endMinute = 0;
    [ObservableProperty] private bool _dayMon = true;
    [ObservableProperty] private bool _dayTue = true;
    [ObservableProperty] private bool _dayWed = true;
    [ObservableProperty] private bool _dayThu = true;
    [ObservableProperty] private bool _dayFri = true;
    [ObservableProperty] private bool _daySat;
    [ObservableProperty] private bool _daySun;

    public ScheduleViewModel(
        IServiceScopeFactory scopeFactory,
        ISchedulerService schedulerService,
        ILogger<ScheduleViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _schedulerService = schedulerService;
        _logger = logger;
    }

    partial void OnSelectedAppChanged(BlockedAppItem? value)
    {
        Schedules.Clear();
        StatusMessage = string.Empty;
        if (value is not null)
            _ = LoadSchedulesAsync(value.Id);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var list = await db.BlockedApps.OrderBy(a => a.Name).ToListAsync();

            Apps = new ObservableCollection<BlockedAppItem>(
                list.Select(a => new BlockedAppItem { Id = a.Id, Name = a.Name, IsEnabled = a.IsEnabled }));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadSchedulesAsync(int appId)
    {
        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var list = await db.BlockSchedules
                .Where(s => s.BlockedAppId == appId)
                .OrderBy(s => s.Id)
                .ToListAsync();

            Schedules = new ObservableCollection<ScheduleItem>(
                list.Select(s => new ScheduleItem
                {
                    Id = s.Id,
                    BlockedAppId = s.BlockedAppId,
                    CronExpression = s.CronExpression,
                    IsEnabled = s.IsEnabled,
                    EnablesBlocking = s.EnablesBlocking,
                    Label = FormatCron(s.CronExpression, s.EnablesBlocking),
                }));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private async Task AddWindow()
    {
        if (SelectedApp is null)
        {
            StatusMessage = "Оберіть програму зі списку.";
            return;
        }

        var days = BuildDaysString();
        if (days is null)
        {
            StatusMessage = "Оберіть хоча б один день тижня.";
            return;
        }

        if (!IsValidTime(StartHour, StartMinute) || !IsValidTime(EndHour, EndMinute))
        {
            StatusMessage = "Перевірте значення часу (год 0-23, хв 0-59).";
            return;
        }

        string startCron = BuildCron(StartHour, StartMinute, days);
        string endCron = BuildCron(EndHour, EndMinute, days);

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.BlockSchedules.AddRange(
                new BlockSchedule { BlockedAppId = SelectedApp.Id, CronExpression = startCron, IsEnabled = true, EnablesBlocking = true },
                new BlockSchedule { BlockedAppId = SelectedApp.Id, CronExpression = endCron, IsEnabled = true, EnablesBlocking = false });
            await db.SaveChangesAsync();

            _logger.LogInformation(
                "Added schedule window for app {AppId}: {Start} → {End}.",
                SelectedApp.Id, startCron, endCron);

            await _schedulerService.ScheduleAppAsync(SelectedApp.Id);
            await LoadSchedulesAsync(SelectedApp.Id);
            StatusMessage = "Розклад додано.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add schedule window.");
            StatusMessage = "Помилка при додаванні розкладу.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private async Task RemoveSchedule(ScheduleItem item)
    {
        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var entity = await db.BlockSchedules.FindAsync(item.Id);
            if (entity is null) return;

            db.BlockSchedules.Remove(entity);
            await db.SaveChangesAsync();

            _logger.LogInformation("Removed schedule {Id} for app {AppId}.", item.Id, item.BlockedAppId);

            await _schedulerService.UnscheduleAppAsync(item.BlockedAppId);
            await _schedulerService.ScheduleAppAsync(item.BlockedAppId);
            await LoadSchedulesAsync(item.BlockedAppId);
            StatusMessage = "Розклад видалено.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove schedule {Id}.", item.Id);
            StatusMessage = "Помилка при видаленні розкладу.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private async Task ToggleSchedule(ScheduleItem item)
    {
        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var entity = await db.BlockSchedules.FindAsync(item.Id);
            if (entity is null) return;

            entity.IsEnabled = !entity.IsEnabled;
            await db.SaveChangesAsync();

            await _schedulerService.UnscheduleAppAsync(item.BlockedAppId);
            await _schedulerService.ScheduleAppAsync(item.BlockedAppId);
            await LoadSchedulesAsync(item.BlockedAppId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle schedule {Id}.", item.Id);
            StatusMessage = "Помилка при зміні стану розкладу.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanModify() => !IsLoading;

    private string? BuildDaysString()
    {
        var parts = new List<string>(7);
        if (DayMon) parts.Add("MON");
        if (DayTue) parts.Add("TUE");
        if (DayWed) parts.Add("WED");
        if (DayThu) parts.Add("THU");
        if (DayFri) parts.Add("FRI");
        if (DaySat) parts.Add("SAT");
        if (DaySun) parts.Add("SUN");
        return parts.Count == 0 ? null : string.Join(",", parts);
    }

    private static string BuildCron(int hour, int minute, string days) =>
        $"0 {minute} {hour} ? * {days}";

    private static bool IsValidTime(int hour, int minute) =>
        hour is >= 0 and <= 23 && minute is >= 0 and <= 59;

    private static string FormatCron(string cron, bool enables)
    {
        var parts = cron.Split(' ');
        if (parts.Length < 6) return cron;

        string time = $"{int.Parse(parts[2]):D2}:{int.Parse(parts[1]):D2}";
        string days = parts[5]
            .Replace("MON", "пн").Replace("TUE", "вт").Replace("WED", "ср")
            .Replace("THU", "чт").Replace("FRI", "пт").Replace("SAT", "сб").Replace("SUN", "нд");
        string action = enables ? "Увімкнути" : "Вимкнути";
        return $"{action}  {time}  ({days})";
    }
}
