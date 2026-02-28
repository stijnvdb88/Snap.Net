using System;
using System.Threading.Tasks;
using SnapDotNet.ControlClient.JsonRpcData;

namespace Snap.Net.Avalonia.Contracts.Services;

public interface IControlClientService
{
    Task InitializeAsync(string ip, int port, int timeout = 3000);

    ServerData? GetServerData();
    event Action OnServerUpdated;
}