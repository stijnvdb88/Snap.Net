using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Snap.Net.Avalonia.Contracts.Services;

namespace Snap.Net.Avalonia.Services;

public class StorageService : IStorageService
{
    public async Task<string?> OpenFilePickerAsync(string title, IEnumerable<string>? allowedExtensions = null)
    {
        Window? window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                         ?.Windows
                         .OfType<Window>()
                         .FirstOrDefault(w => w.IsActive)
                         ?? (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                         ?.MainWindow;
        if (window == null)
        {
            return null;
        }

        FilePickerOpenOptions options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = allowedExtensions == null ? null :
            [
                new FilePickerFileType("Executable")
                {
                    Patterns = allowedExtensions.Select(e => $"*.{e}").ToArray()
                }
            ]
        };

        IReadOnlyList<IStorageFile> result = await window.StorageProvider.OpenFilePickerAsync(options);
        return result.FirstOrDefault()?.TryGetLocalPath();
    }
}