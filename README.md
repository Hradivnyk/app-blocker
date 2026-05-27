# AppBlocker

A Windows application for blocking program launches on a schedule. Lives in the system tray, starts with Windows, and silently terminates blocked applications during specified time intervals.

## Features

- Block any application on a schedule (Cron expressions)
- Instant notification when a blocked app is launched, showing the unblock time
- Temporary unblock of an individual app for 1–15 minutes (with 30-second confirmation timer)
- Stop the current blocking session until the end of the interval (with 60-second confirmation timer)
- Auto-startup with Windows without showing a window
- One app can have multiple independent schedules

## Stack

| Component | Technology |
|-----------|-----------|
| Language | C# 12, .NET 8 |
| UI | WPF + MVVM (CommunityToolkit.Mvvm) |
| Tray | H.NotifyIcon.Wpf |
| Database | SQLite + Entity Framework Core 8 |
| Scheduler | Quartz.NET 3 |
| DI | Microsoft.Extensions.Hosting |
| Logging | Serilog (file + Debug) |

## Requirements

- Windows 10 / 11
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (or use self-contained publish)

## Build & Run

```bash
# Clone the repository
git clone <repo-url>
cd apps-blocker

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run --project src/AppBlocker
```

## Publish (self-contained .exe)

```bash
dotnet publish src/AppBlocker -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The output file will be in `src/AppBlocker/bin/Release/net8.0-windows/win-x64/publish/`.

## Data Storage

The SQLite database is stored at `%AppData%\AppBlocker\appblocker.db` and is created automatically on first run.

## Auto-startup

Auto-startup can be enabled in the **Settings** tab. The app writes itself to the registry (`HKCU\...\Run`) and starts minimized to the tray without showing a window.

## Notes

- Administrator rights are **not required** to block most applications.
- Windows Defender may react to process termination — this is expected behavior.
- The app runs as a single instance; launching it again activates the already running instance.
