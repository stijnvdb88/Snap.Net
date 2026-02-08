namespace Snap.Net.Avalonia.Contracts.Services;

public interface ISettingsService
{
    void Save();
    T? Get<T>(string key, T? defaultValue = default);
    void Set<T>(string key, T value, bool save = true);
}