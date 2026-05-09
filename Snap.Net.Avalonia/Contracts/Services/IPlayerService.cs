using System.Threading.Tasks;
using Snap.Net.Avalonia.ViewModels.Player;

namespace Snap.Net.Avalonia.Contracts.Services;

public interface IPlayerService
{
    Task<PlayerDeviceViewModel[]> GetDevicesAsync(bool includeDefault = false);
    Task TogglePlay(PlayerDeviceViewModel playerDevice);
    bool IsPlaying(PlayerDeviceViewModel playerDevice);
}