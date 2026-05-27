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

### Using the publish profile (recommended)

A reusable publish profile is included at `src/AppBlocker/Properties/PublishProfiles/win-x64.pubxml`.

From the solution root:

```bash
dotnet publish src/AppBlocker -p:PublishProfile=win-x64
```

From inside the project directory:

```bash
dotnet publish -p:PublishProfile=win-x64
```

From Visual Studio: open the Publish wizard (`Build → Publish AppBlocker`), select the existing `win-x64` profile, and click **Publish**.

Output: `src/AppBlocker/publish/win-x64/AppBlocker.exe`

### CLI alternative (without the profile)

```bash
dotnet publish src/AppBlocker -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none
```

### Output

| File | Notes |
|---|---|
| `AppBlocker.exe` | Single self-contained executable (~80–120 MB) |

The .NET 8 runtime and all managed assemblies are bundled inside the EXE — copy just the one file to the target machine, no installer needed.

### SQLite native library note

SQLite uses a native DLL (`e_sqlite3.dll`) that cannot be embedded inside the EXE itself. The publish profile sets `IncludeNativeLibrariesForSelfExtract=true`, so .NET extracts it to `%TEMP%\.net\AppBlocker\<hash>\` on first launch and reuses it on subsequent runs. This is invisible to the user and completes in under a second. If the extraction folder is deleted (e.g. by a disk-cleanup tool), it is recreated automatically on next launch.

### Windows Defender note

AppBlocker terminates processes by design. Windows Defender may flag the executable for this behaviour or for the self-extracting native library — this is a false positive. If Defender blocks the app:

1. Open **Windows Security → Virus & threat protection → Protection history**.
2. Find the quarantined item and choose **Restore** or **Allow**.
3. Add the publish folder as an exclusion if the false positive recurs.

Running from a developer build (`dotnet run`) is unaffected.

## Data Storage

The SQLite database is stored at `%AppData%\AppBlocker\appblocker.db` and is created automatically on first run.

## Auto-startup

Auto-startup can be enabled in the **Settings** tab. The app writes itself to the registry (`HKCU\...\Run`) and starts minimized to the tray without showing a window.

## Notes

- Administrator rights are **not required** to block most applications.
- Windows Defender may react to process termination — this is expected behavior.
- The app runs as a single instance; launching it again activates the already running instance.
