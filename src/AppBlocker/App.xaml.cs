using System.IO;
using System.Threading;
using System.Windows;
using AppBlocker.Data;
using AppBlocker.Services;
using AppBlocker.Services.Jobs;
using AppBlocker.Tray;
using AppBlocker.ViewModels;
using AppBlocker.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Serilog;

namespace AppBlocker;

public partial class App : Application
{
    private IHost? _host;
    private Mutex? _singleInstanceMutex;
    private const string MutexName = "AppBlocker_SingleInstance";

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(initiallyOwned: true, name: MutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show(
                "AppBlocker is already running. Check the system tray.",
                "AppBlocker",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            _singleInstanceMutex.Dispose();
            Shutdown();
            return;
        }

        bool startMinimized = e.Args.Contains("--minimized", StringComparer.OrdinalIgnoreCase);

        string logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AppBlocker", "Logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: Path.Combine(logDirectory, "appblocker-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .WriteTo.Debug()
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((_, services) =>
            {
                string dbDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AppBlocker");
                Directory.CreateDirectory(dbDirectory);
                string dbPath = Path.Combine(dbDirectory, "appblocker.db");

                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"),
                    ServiceLifetime.Transient);

                services.AddQuartz();
                services.AddQuartzHostedService(options =>
                {
                    options.WaitForJobsToComplete = true;
                });
                services.AddTransient<BlockingScheduleJob>();

                services.AddSingleton<IBlockerService, BlockerService>();
                services.AddSingleton<ISchedulerService, SchedulerService>();
                services.AddSingleton<IStartupService, StartupService>();
                services.AddSingleton<ProcessWatcherService>();
                services.AddSingleton<TrayIconManager>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<TrayIconManager>>();
                    var app = (App)Application.Current;
                    return new TrayIconManager(logger, app.ShowMainWindow, Application.Current.Shutdown);
                });

                services.AddTransient<MainViewModel>();
                services.AddTransient<AppsViewModel>();
                services.AddTransient<ScheduleViewModel>();
                services.AddTransient<SettingsViewModel>();

                services.AddTransient<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        try
        {
            await using var scope = _host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            Log.Information("Database migrations applied.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply database migrations.");
        }

        var tray = _host.Services.GetRequiredService<TrayIconManager>();
        tray.Initialize();

        var blocker = _host.Services.GetRequiredService<IBlockerService>();
        await blocker.StartAsync();

        var schedulerService = _host.Services.GetRequiredService<ISchedulerService>();
        await schedulerService.InitializeAsync();

        if (!startMinimized)
        {
            ShowMainWindow();
        }

        Log.Information("AppBlocker started. StartMinimized={StartMinimized}", startMinimized);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Information("AppBlocker shutting down.");

        if (_host is not null)
        {
            var blocker = _host.Services.GetRequiredService<IBlockerService>();
            await blocker.StopAsync();

            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();

        await Log.CloseAndFlushAsync();

        base.OnExit(e);
    }

    public void ShowMainWindow()
    {
        if (MainWindow is null)
        {
            MainWindow = _host!.Services.GetRequiredService<MainWindow>();
        }
        MainWindow.Show();
        MainWindow.Activate();
    }

    public static IServiceProvider Services =>
        ((App)Current)._host!.Services;
}
