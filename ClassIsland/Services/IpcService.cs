using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.Ipc;
using ClassIsland.Shared.IPC;
using dotnetCampus.Ipc.Context;
using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using dotnetCampus.Ipc.Pipes;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class IpcService : IIpcService
{
    public ILogger<IpcService> Logger { get; }
    public IpcProvider IpcProvider { get; }
    public JsonIpcDirectRoutedProvider JsonRoutedProvider { get; }

    private ObservableCollection<IpcPeer?> ConnectedPeers { get; } = [];

    public IpcService(ILogger<IpcService> logger)
    {
        Logger = logger;
        IpcProvider = new IpcProvider(IpcClient.PipeName);
        IpcProvider.PeerConnected += IpcProviderOnPeerConnected;
        JsonRoutedProvider = new JsonIpcDirectRoutedProvider(IpcProvider);
    }

    private async void IpcProviderOnPeerConnected(object? sender, PeerConnectedArgs e)
    {
        Logger.LogInformation("对等端 {} 已连接。", e.Peer.PeerName);
        var jsonPeer = await JsonRoutedProvider.GetAndConnectClientAsync(e.Peer.PeerName);
        var ipcPeer = new IpcPeer(e.Peer, jsonPeer);
        ConnectedPeers.Add(ipcPeer);
        e.Peer.PeerConnectionBroken += (o, args) =>
        {
            ConnectedPeers.Remove(ipcPeer);
            Logger.LogInformation("对等端 {} 已断开。", e.Peer.PeerName);
        };
    }

    public async Task BroadcastNotificationAsync(string id)
    {
        ConnectedPeers.Remove(null);
        foreach (var i in ConnectedPeers)
        {
            if (i?.JsonPeerProxy != null) 
                await i.JsonPeerProxy.NotifyAsync(id);
        }
    }

    public async Task BroadcastNotificationAsync<T>(string id, T obj) where T : class
    {
        ConnectedPeers.Remove(null);
        foreach (var i in ConnectedPeers)
        {
            if (i?.JsonPeerProxy != null) 
                await i.JsonPeerProxy.NotifyAsync(id, obj);
        }
    }
}