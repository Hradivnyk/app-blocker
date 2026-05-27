# AppBlocker — CLAUDE.md

## Project Description

A Windows application for blocking program launches on a schedule. Starts with the system, lives in the system tray, and provides a UI for managing the list of blocked applications and blocking schedules.

---

## Stack

- **Language:** C# 12, .NET 8
- **UI Framework:** WPF (XAML)
- **UI Architecture:** MVVM (CommunityToolkit.Mvvm)
- **Tray Icon:** `H.NotifyIcon.Wpf` (native WPF, no WindowsFormsIntegration)
- **Database:** SQLite via Entity Framework Core 8
- **Scheduler:** Quartz.NET
- **DI Container:** Microsoft.Extensions.DependencyInjection
- **Logging:** Microsoft.Extensions.Logging + Serilog (file + Debug output)

---

## Project Structure

```
AppBlocker/
├── CLAUDE.md
├── AppBlocker.sln
├── src/
│   └── AppBlocker/
│       ├── App.xaml                    # Entry point; main window is NOT shown on startup
│       ├── App.xaml.cs                 # DI, tray, and service initialization
│       │
│       ├── Models/
│       │   ├── BlockedApp.cs           # Blocked application model (EF Entity)
│       │   └── BlockSchedule.cs        # Schedule model (EF Entity)
│       │
│       ├── Data/
│       │   ├── AppDbContext.cs         # EF Core DbContext
│       │   └── Migrations/             # EF migrations (do not edit manually)
│       │
│       ├── Services/
│       │   ├── IBlockerService.cs
│       │   ├── BlockerService.cs       # Process monitoring + kill logic
│       │   ├── ISchedulerService.cs
│       │   ├── SchedulerService.cs     # Quartz.NET jobs; enables/disables blocking
│       │   ├── IStartupService.cs
│       │   ├── StartupService.cs       # Registry write/remove (HKCU Run)
│       │   └── ProcessWatcherService.cs # Win32 WMI subscription for process creation
│       │
│       ├── Tray/
│       │   ├── TrayIconManager.cs      # NotifyIcon, context menu
│       │   └── TrayContextMenu.cs      # Tray menu items
│       │
│       ├── ViewModels/
│       │   ├── MainViewModel.cs
│       │   ├── AppsViewModel.cs        # Blocked apps list
│       │   ├── ScheduleViewModel.cs    # Schedule management
│       │   └── SettingsViewModel.cs    # Auto-startup
│       │
│       ├── Views/
│       │   ├── MainWindow.xaml         # Shell window with navigation
│       │   ├── AppsView.xaml
│       │   ├── ScheduleView.xaml
│       │   └── SettingsView.xaml
│       │
│       ├── Converters/                 # IValueConverter implementations for XAML
│       ├── Resources/
│       │   ├── Icons/                  # .ico files for tray
│       │   └── Styles/                 # ResourceDictionary with WPF styles
│       │
│       └── AppBlocker.csproj
```

---

## Key Implementation Rules

### Process Blocking

- Primary mechanism: `WMI Win32_ProcessStartTrace` via `ManagementEventWatcher` — subscribes to process start events, not polling.
- Fallback: `System.Threading.Timer` with 2-second interval + `Process.GetProcessesByName()` if WMI is unavailable.
- Use `process.Kill()` for killing + verify the process is gone via `process.HasExited`.
- **Never** kill processes with `SessionId != Environment.SessionId` (to avoid touching system processes).
- Store exe names without extension (e.g. `chrome`, not `chrome.exe`).
- After killing, show a WPF notification window (topmost, no taskbar button) with a blocking message and unblock time (e.g. _"Chrome is blocked until 18:00"_). The window closes with an "OK" button or automatically after 10 seconds. Display time in local time.

### Scheduler (Quartz.NET)

- Each `BlockSchedule` is a separate Quartz Job with a CronTrigger.
- Cron format: Quartz 6-field (`seconds minutes hours day-of-month month day-of-week`), e.g. `0 0 9 ? * MON-FRI`.
- On app startup, restore all active schedules from the database.
- Use `IScheduler.PauseAll()` / `IScheduler.ResumeAll()` for global pause/resume.
- Time zone: always store in UTC, display in local time.

### Tray and Window

- `App.xaml`: set `ShutdownMode="OnExplicitShutdown"`.
- Create the main window **lazily** — only on first open from tray.
- On window close (`Window.Closing`) — hide (`Hide()`), don't close.
- True shutdown only via the "Exit" item in the tray context menu.
- Double-click on tray icon — show/hide window.

### Auto-startup

- Registry path: `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
- Key: `AppBlocker`
- Value: `"{path to exe}" --minimized`
- Handle the `--minimized` argument in `App.xaml.cs` — do not show window on startup.

### Database

- SQLite file: `%AppData%\AppBlocker\appblocker.db`
- Apply migrations automatically on startup (`context.Database.MigrateAsync()`).
- `BlockedApp` and `BlockSchedule` — one-to-many relationship (one app can have multiple schedules).

### Temporary Unblocking

Available from the app UI (tab or list item context menu):

- **Unblock an app temporarily** — user enters a duration from 1 to 15 minutes. Before activation, a 30-second countdown timer is shown (cannot be skipped). After the countdown, blocking for that app is temporarily lifted for the specified duration, then automatically restored.
- **Stop the current blocking session** — stops all active blocking until the end of the current scheduled interval (`IScheduler.PauseAll()`). Before activation, a 60-second countdown timer is shown (cannot be skipped). After the interval ends, the schedule resumes normally.
- During the countdown, the confirm button is disabled; there is a "Cancel" button to abort.
- Both timers must be implemented via `DispatcherTimer` in the ViewModel, displayed in `0:30` / `1:00` format counting down.

### Bypass Protection (optional)

- Windows Job Objects via P/Invoke (`CreateJobObject`, `AssignProcessToJobObject`) — prevents child processes from bypassing blocking.

---

## NuGet Packages

```xml
<!-- src/AppBlocker/AppBlocker.csproj -->
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.*" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.*" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.*" />
<PackageReference Include="Quartz" Version="3.*" />
<PackageReference Include="Quartz.Extensions.DependencyInjection" Version="3.*" />
<PackageReference Include="Serilog.Extensions.Logging" Version="8.*" />
<PackageReference Include="Serilog.Sinks.File" Version="5.*" />
<PackageReference Include="H.NotifyIcon.Wpf" Version="2.*" />
```

---

## Development Commands

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run --project src/AppBlocker

# Tests
dotnet test

# Add EF migration
dotnet ef migrations add <MigrationName> --project src/AppBlocker

# Apply migrations manually
dotnet ef database update --project src/AppBlocker

# Publish (self-contained exe)
dotnet publish src/AppBlocker -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## Important Constraints and Pitfalls

1. **Administrator rights** — killing some processes requires admin rights. By default the app runs without admin rights (user-level). If killing system/protected processes is needed — add a manifest with `requireAdministrator`. But **do not do this by default** — it will break auto-startup via HKCU Run.

2. **WMI subscriptions** — may not work without admin rights depending on the Windows version. Always have a fallback to polling.

3. **Single instance** — the app must run as a single instance. Use a `Mutex` on startup:
   ```csharp
   var mutex = new Mutex(true, "AppBlocker_SingleInstance", out bool isNew);
   if (!isNew) { /* show existing window and exit */ }
   ```

4. **Antivirus** — killing processes may trigger Windows Defender. This is expected; document it for the user.

5. **EF Core + WPF** — make DbContext calls on a background thread; marshal results to the UI thread via `Application.Current.Dispatcher`.

6. **Quartz.NET and DI** — Jobs must be registered as `Transient` in DI, not `Singleton`.

---

## README.md

The `README.md` file is in the project root and is the primary documentation for users and developers. Whenever functionality, stack, build commands, or important constraints change — update `README.md` accordingly.

---

## Git Conventions

### Branches (Git Flow)

- `main` — stable release
- `develop` — main development branch
- `feature/<name>` — new functionality (from `develop`)
- `fix/<name>` — bug fixes (from `develop`)
- `hotfix/<name>` — urgent production fixes (from `main`)
- `release/<version>` — release preparation (from `develop`)

### Commits (Conventional Commits)

Format: `<type>(<scope>): <short description>`

Types:
- `feat` — new feature
- `fix` — bug fix
- `refactor` — refactoring without behavior change
- `style` — formatting, indentation (no logic changes)
- `test` — tests
- `docs` — documentation
- `chore` — dependencies, configs, build

Rules:
- Description — concise, in the format `what was done` (no lengthy explanations)
- **Never** add `Co-Authored-By` or any mention of Claude authorship in commits
- **Split large changes into logical commits** — each commit should represent one coherent, self-contained change (e.g. model change, service logic, UI update). Do not bundle unrelated changes into a single commit. If a feature touches multiple layers (model → service → UI), commit each layer separately with a descriptive message.
- Examples:
  - `feat(blocker): add WMI process watcher`
  - `fix(scheduler): restore jobs on app startup`
  - `refactor(tray): lazy init main window`
  - `chore: add Quartz.NET and EF Core packages`

---

## Code Conventions

- Naming: `PascalCase` for public members, `_camelCase` for private fields.
- All public methods in services — `async Task`, even if currently synchronous.
- ViewModels inherit from `ObservableObject` (CommunityToolkit.Mvvm).
- Commands — `[RelayCommand]` attribute (CommunityToolkit.Mvvm source generator).
- Do not use `code-behind` in Views, except for initialization (`InitializeComponent`).
- Log all blocking operations (what, when, which app).

---

## Implementation Order (recommended)

1. Basic project structure + DI + App.xaml without window
2. TrayIcon + context menu + open/close window
3. Auto-startup (registry) + `--minimized` handling
4. EF Core + models + migrations
5. BlockerService (polling variant)
6. Basic UI: app list + add via browse
7. SchedulerService + Quartz jobs
8. Schedule UI
9. WMI subscription instead of polling
10. Settings
11. Publish as single-file exe
