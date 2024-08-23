using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared.IPC;
using dotnetCampus.Ipc.Context;
using dotnetCampus.Ipc.Pipes;
using dotnetCampus.Ipc.Utils.Logging;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class IpcService : IIpcService
{
    public ILogger<IpcService> Logger { get; }
    public IpcProvider IpcProvider { get; }

    public IpcService(ILogger<IpcService> logger)
    {
        Logger = logger;
        IpcProvider = new IpcProvider(IpcClient.PipeName);
        IpcProvider.PeerConnected += IpcProviderOnPeerConnected;
    }

    private void IpcProviderOnPeerConnected(object? sender, PeerConnectedArgs e)
    {
        Logger.LogInformation("对等端 {} 已连接。", e.Peer.PeerName);
    }
}