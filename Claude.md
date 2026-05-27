# AppBlocker — CLAUDE.md

## Опис проекту

Windows-додаток для блокування запуску програм за розкладом. Запускається з системою, живе в system tray, надає UI для керування списком заблокованих програм та розкладом блокування.

---

## Стек

- **Мова:** C# 12, .NET 8
- **UI Framework:** WPF (XAML)
- **Архітектура UI:** MVVM (CommunityToolkit.Mvvm)
- **Tray-іконка:** `H.NotifyIcon.Wpf` (нативний WPF, без WindowsFormsIntegration)
- **База даних:** SQLite через Entity Framework Core 8
- **Розклад:** Quartz.NET
- **DI-контейнер:** Microsoft.Extensions.DependencyInjection
- **Логування:** Microsoft.Extensions.Logging + Serilog (файл + Debug output)
---

## Структура проекту

```
AppBlocker/
├── CLAUDE.md
├── AppBlocker.sln
├── src/
│   └── AppBlocker/
│       ├── App.xaml                    # Точка входу; головне вікно НЕ показується при старті
│       ├── App.xaml.cs                 # Ініціалізація DI, tray, сервісів
│       │
│       ├── Models/
│       │   ├── BlockedApp.cs           # Модель заблокованої програми (EF Entity)
│       │   └── BlockSchedule.cs        # Модель розкладу (EF Entity)
│       │
│       ├── Data/
│       │   ├── AppDbContext.cs         # EF Core DbContext
│       │   └── Migrations/             # EF міграції (не редагувати вручну)
│       │
│       ├── Services/
│       │   ├── IBlockerService.cs
│       │   ├── BlockerService.cs       # Моніторинг процесів + kill логіка
│       │   ├── ISchedulerService.cs
│       │   ├── SchedulerService.cs     # Quartz.NET jobs; вмикає/вимикає блокування
│       │   ├── IStartupService.cs
│       │   ├── StartupService.cs       # Запис/видалення з реєстру (HKCU Run)
│       │   └── ProcessWatcherService.cs # Win32 WMI-підписка на створення процесів
│       │
│       ├── Tray/
│       │   ├── TrayIconManager.cs      # NotifyIcon, контекстне меню
│       │   └── TrayContextMenu.cs      # Пункти меню tray
│       │
│       ├── ViewModels/
│       │   ├── MainViewModel.cs
│       │   ├── AppsViewModel.cs        # Список заблокованих програм
│       │   ├── ScheduleViewModel.cs    # Керування розкладом
│       │   └── SettingsViewModel.cs    # Автозапуск
│       │
│       ├── Views/
│       │   ├── MainWindow.xaml         # Shell-вікно з навігацією
│       │   ├── AppsView.xaml
│       │   ├── ScheduleView.xaml
│       │   └── SettingsView.xaml
│       │
│       ├── Converters/                 # IValueConverter реалізації для XAML
│       ├── Resources/
│       │   ├── Icons/                  # .ico файли для tray
│       │   └── Styles/                 # ResourceDictionary зі стилями WPF
│       │
│       └── AppBlocker.csproj
```

---

## Ключові правила реалізації

### Блокування процесів

- Основний механізм: `WMI Win32_ProcessStartTrace` через `ManagementEventWatcher` — підписка на старт процесів, не polling.
- Fallback: `System.Threading.Timer` з інтервалом 2 сек + `Process.GetProcessesByName()` якщо WMI недоступний.
- Для kill використовувати `process.Kill()` + перевірити чи процес ще живий через `process.HasExited`.
- **Ніколи** не вбивати процеси з `SessionId != Environment.SessionId` (щоб не зачепити системні).
- Зберігати ім'я exe без розширення (наприклад `chrome`, не `chrome.exe`).
- Після kill показувати WPF-вікно сповіщення (topmost, без taskbar кнопки) з повідомленням про блокування та часом розблокування (наприклад: _"Chrome заблоковано до 18:00"_). Вікно закривається кнопкою "OK" або автоматично через 10 секунд. Час відображати в local time.

### Розклад (Quartz.NET)

- Кожен `BlockSchedule` — окремий Quartz Job з CronTrigger.
- Формат Cron: Quartz 6-field (`seconds minutes hours day-of-month month day-of-week`), наприклад `0 0 9 ? * MON-FRI`.
- При старті додатку відновлювати всі активні розклади з БД.
- Використовувати `IScheduler.PauseAll()` / `IScheduler.ResumeAll()` для глобального призупинення.
- Часовий пояс: завжди зберігати в UTC, відображати в local time.

### Tray та вікно

- `App.xaml`: встановити `ShutdownMode="OnExplicitShutdown"`.
- Головне вікно створювати **лінивo** — тільки при першому відкритті з tray.
- При закритті вікна (`Window.Closing`) — ховати (`Hide()`), не закривати.
- Справжнє закриття тільки через пункт "Вийти" в контекстному меню tray.
- Подвійний клік на tray — показати/сховати вікно.

### Автозапуск

- Шлях до реєстру: `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
- Ключ: `AppBlocker`
- Значення: `"{шлях до exe}" --minimized`
- Обробляти аргумент `--minimized` в `App.xaml.cs` — не показувати вікно при старті.

### База даних

- SQLite файл: `%AppData%\AppBlocker\appblocker.db`
- Міграції застосовувати автоматично при старті (`context.Database.MigrateAsync()`).
- `BlockedApp` і `BlockSchedule` — зв'язок один-до-багатьох (одна програма може мати кілька розкладів).

### Тимчасове розблокування

Доступно з UI додатку (вкладка або контекстне меню елемента списку):

- **Розблокувати програму на час** — користувач вводить тривалість від 1 до 15 хвилин. Перед активацією показується таймер зворотного відліку 30 секунд (який не можна пропустити). Після відліку блокування для цієї програми тимчасово знімається на вказаний час, після чого автоматично відновлюється.
- **Зупинити сесію блокування** — зупиняє всі активні блокування до кінця поточного запланованого інтервалу (`IScheduler.PauseAll()`). Перед активацією показується таймер зворотного відліку 60 секунд (який не можна пропустити). Після закінчення інтервалу розклад відновлюється в штатному режимі.
- Під час відліку кнопка підтвердження неактивна; є кнопка "Скасувати" для відміни.
- Обидва таймери реалізувати через `DispatcherTimer` у ViewModel, відображати у форматі `0:30` / `1:00` з відліком донизу.

### Захист від обходу (опційно)

- Windows Job Objects через P/Invoke (`CreateJobObject`, `AssignProcessToJobObject`) — не дають дочірнім процесам обійти блокування.

---

## NuGet пакети

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

## Команди розробки

```bash
# Відновити залежності
dotnet restore

# Збірка
dotnet build

# Запуск
dotnet run --project src/AppBlocker

# Тести
dotnet test

# Додати міграцію EF
dotnet ef migrations add <MigrationName> --project src/AppBlocker

# Застосувати міграції вручну
dotnet ef database update --project src/AppBlocker

# Публікація (self-contained exe)
dotnet publish src/AppBlocker -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## Важливі обмеження та підводні камені

1. **Права адміністратора** — для вбивства деяких процесів потрібні права адміна. За замовчуванням додаток працює без прав адміна (user-level). Якщо потрібне вбивство системних/захищених процесів — додати маніфест з `requireAdministrator`. Але **не робити це за замовчуванням** — це зламає автозапуск через HKCU Run.

2. **WMI підписки** — можуть не спрацювати без прав адміна залежно від версії Windows. Завжди мати fallback до polling.

3. **Single instance** — додаток має бути в одному екземплярі. Використовувати `Mutex` при старті:
   ```csharp
   var mutex = new Mutex(true, "AppBlocker_SingleInstance", out bool isNew);
   if (!isNew) { /* показати існуюче вікно і вийти */ }
   ```

4. **Антивіруси** — kill процесів може тригерити Windows Defender. Це нормально, але варто задокументувати для користувача.

5. **EF Core + WPF** — виклики до DbContext робити в background thread, результати маршалити в UI thread через `Application.Current.Dispatcher`.

6. **Quartz.NET і DI** — Jobs мають бути зареєстровані як `Transient` в DI, не `Singleton`.

---

## README.md

Файл `README.md` знаходиться в корені проекту і є основною документацією для користувачів та розробників. При будь-яких змінах у функціональності, стеку, командах збірки або важливих обмеженнях — оновлювати `README.md` відповідно.

---

## Конвенції коду

- Неймінг: `PascalCase` для публічних членів, `_camelCase` для приватних полів.
- Усі публічні методи в сервісах — `async Task`, навіть якщо зараз синхронні.
- ViewModels наслідувати від `ObservableObject` (CommunityToolkit.Mvvm).
- Команди — `[RelayCommand]` атрибут (CommunityToolkit.Mvvm source generator).
- Не використовувати `code-behind` у Views, крім ініціалізації (`InitializeComponent`).
- Логувати всі операції блокування (що, коли, яка програма).

---

## Порядок реалізації (рекомендований)

1. Базова структура проекту + DI + App.xaml без вікна
2. TrayIcon + контекстне меню + відкриття/закриття вікна
3. Автозапуск (реєстр) + обробка `--minimized`
4. EF Core + моделі + міграції
5. BlockerService (polling-варіант)
6. Базовий UI: список програм + додавання через browse
7. SchedulerService + Quartz jobs
8. UI для розкладу
9. WMI-підписка замість polling
10. Налаштування
11. Публікація як single-file exe