using System.Collections.Generic;
using System.Threading.Tasks;

namespace Snap.Net.Avalonia.Contracts.Services;

public interface IStorageService
{
    Task<string?> OpenFilePickerAsync(string title, IEnumerable<string>? allowedExtensions = null);
}