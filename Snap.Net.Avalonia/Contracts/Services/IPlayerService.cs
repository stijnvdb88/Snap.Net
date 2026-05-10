using System;
using System.Threading;
using System.Threading.Tasks;
using Snap.Net.Avalonia.ViewModels.Player;

namespace Snap.Net.Avalonia.Contracts.Services;

public record SnapserverEndpoint(string Host, int Port);

public interface IPlayerService
{
    Task<PlayerDeviceViewModel[]> GetDevicesAsync(bool includeDefault = false);
    Task Play(PlayerDeviceViewModel playerDevice);
    Task TogglePlay(PlayerDeviceViewModel playerDevice);
    bool IsPlaying(PlayerDeviceViewModel playerDevice);

    void StopAll();
    Task<SnapserverEndpoint[]> DiscoverSnapserversAsync(CancellationToken cancellationToken);

    string GetSnapclientArgs(PlayerDeviceViewModel playerDevice);

    string SnapclientPath { get; }
    string? SnapclientVersion { get; }

    Task ValidateSnapclientPath();
}