using System.Collections.ObjectModel;
using System.IO;
using AppBlocker.Data;
using AppBlocker.Models;
using AppBlocker.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace AppBlocker.ViewModels;

public partial class AppsViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBlockerService _blockerService;
    private readonly ISchedulerService _schedulerService;
    private readonly ILogger<AppsViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<BlockedAppItem> _apps = new();

    [ObservableProperty]
    private BlockedAppItem? _selectedApp;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public AppsViewModel(IServiceScopeFactory scopeFactory, IBlockerService blockerService, ISchedulerService schedulerService, ILogger<AppsViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _blockerService = blockerService;
        _schedulerService = schedulerService;
        _logger = logger;
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

    [RelayCommand(CanExecute = nameof(CanModify))]
    private async Task AddApp()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Оберіть програму для блокування",
            Filter = "Виконувані файли (*.exe)|*.exe",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog() != true) return;

        var appName = Path.GetFileNameWithoutExtension(dialog.FileName);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        bool exists = await db.BlockedApps.AnyAsync(a => a.Name == appName);
        if (exists)
        {
            StatusMessage = $"'{appName}' вже є в списку.";
            return;
        }

        db.BlockedApps.Add(new BlockedApp { Name = appName, IsEnabled = true });
        await db.SaveChangesAsync();

        _logger.LogInformation("Added blocked app '{Name}'.", appName);
        await LoadAsync();
        StatusMessage = $"'{appName}' додано.";
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private async Task RemoveApp(BlockedAppItem item)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var entity = await db.BlockedApps.FindAsync(item.Id);
        if (entity is null) return;

        db.BlockedApps.Remove(entity);
        await db.SaveChangesAsync();

        await _schedulerService.UnscheduleAppAsync(item.Id);

        _logger.LogInformation("Removed blocked app '{Name}'.", item.Name);
        Apps.Remove(item);
        StatusMessage = $"'{item.Name}' видалено.";
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private async Task ToggleEnabled(BlockedAppItem item)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var entity = await db.BlockedApps.FindAsync(item.Id);
        if (entity is null) return;

        entity.IsEnabled = !entity.IsEnabled;
        await db.SaveChangesAsync();

        if (entity.IsEnabled)
            await _blockerService.EnableBlockingForAppAsync(entity.Id);
        else
            await _blockerService.DisableBlockingForAppAsync(entity.Id);

        await LoadAsync();
    }

    private bool CanModify() => !IsLoading;
}
