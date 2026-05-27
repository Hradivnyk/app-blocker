using AppBlocker.Models;

namespace AppBlocker.Services;

public interface ISettingsService
{
    AppSettings Current { get; }
    Task SaveAsync();
}
