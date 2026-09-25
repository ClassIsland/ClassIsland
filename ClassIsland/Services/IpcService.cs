using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.Ipc;
using ClassIsland.Shared.IPC;
using dotnetCampus.Ipc.Context;
using dotnetCampus.Ipc.Exceptions;
using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using dotnetCampus.Ipc.Pipes;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class IpcService : IIpcService
{
    public ILogger<IpcService> Logger { get; }
    public IpcProvider IpcProvider { get; }
    public JsonIpcDirectRoutedProvider JsonRoutedProvider { get; }

    private List<IpcPeer> ConnectedPeers { get; } = [];
    private object ConnectedPeersLock { get; } = new();

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
        lock (ConnectedPeersLock)
        {
            ConnectedPeers.Add(ipcPeer);
        }
        e.Peer.PeerConnectionBroken += (o, args) =>
        {
            lock (ConnectedPeersLock)
            {
                ConnectedPeers.Remove(ipcPeer);
            }
            Logger.LogInformation("对等端 {} 已断开。", e.Peer.PeerName);
        };
    }

    public async Task BroadcastNotificationAsync(string id)
    {
        foreach (var peer in GetConnectedPeersSnapshot())
        {
            try
            {
                await peer.JsonPeerProxy.NotifyAsync(id);
            }
            catch (Exception ex) when (ex is IpcPeerConnectionBrokenException or
                                       IpcRemoteException { InnerException: IOException or ObjectDisposedException })
            {
                Logger.LogWarning(ex, "向对等端 {PeerName} 广播 {NotificationId} 时发生连接错误。", peer.PeerProxy.PeerName, id);
            }
        }
    }

    public async Task BroadcastNotificationAsync<T>(string id, T obj) where T : class
    {
        foreach (var peer in GetConnectedPeersSnapshot())
        {
            try
            {
                await peer.JsonPeerProxy.NotifyAsync(id, obj);
            }
            catch (Exception ex) when (ex is IpcPeerConnectionBrokenException or
                                       IpcRemoteException { InnerException: IOException or ObjectDisposedException })
            {
                Logger.LogWarning(ex, "向对等端 {PeerName} 广播 {NotificationId} 时发生连接错误。", peer.PeerProxy.PeerName, id);
            }
        }
    }

    private IpcPeer[] GetConnectedPeersSnapshot()
    {
        // 对等端可在异步发送期间连接或断开，不能跨 await 枚举共享集合。
        lock (ConnectedPeersLock)
        {
            return ConnectedPeers.ToArray();
        }
    }
}
