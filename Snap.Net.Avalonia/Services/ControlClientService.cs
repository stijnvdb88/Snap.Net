using System;
using System.Threading.Tasks;
using Snap.Net.Avalonia.Contracts.Services;
using SnapDotNet.ControlClient;
using SnapDotNet.ControlClient.JsonRpcData;

namespace Snap.Net.Avalonia.Services;

public class ControlClientService : IControlClientService
{
    private SnapcastClient m_SnapcastClient;
    public ControlClientService()
    {
        m_SnapcastClient = new  SnapcastClient();
        m_SnapcastClient.OnServerUpdated += _OnServerUpdated;
        SnapcastClient.AutoReconnect = true;
    }
    
    public Task InitializeAsync(string ip, int port, int timeout = 3000)
    {
        return m_SnapcastClient.ConnectAsync(ip, port, timeout);
    }

    private void _OnServerUpdated()
    {
        OnServerUpdated?.Invoke();
    }

    public ServerData GetServerData()
    {
        return  m_SnapcastClient.ServerData;
    }

    public event Action? OnServerUpdated;
}