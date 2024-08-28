using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using dotnetCampus.Ipc.Pipes;

namespace ClassIsland.Shared.IPC;

/// <summary>
/// 跨进程通信客户端，用于在其他进程中与 ClassIsland 本体进行通信。
/// </summary>
public class IpcClient
{
    /// <summary>
    /// IPC 服务端管道名称
    /// </summary>
    public static string PipeName { get; } = "ClassIsland.IPC.v2.Server";

    /// <summary>
    /// IPC 提供方。
    /// </summary>
    public IpcProvider Provider { get; } = new IpcProvider();

    /// <summary>
    /// 远程的对方
    /// </summary>
    public PeerProxy? PeerProxy { get; private set; }

    /// <summary>
    /// JSON IPC 提供方。
    /// </summary>
    public JsonIpcDirectRoutedProvider JsonIpcProvider { get; }
    
    /// <summary>
    /// 初始化一个 <see cref="IpcClient"/> 对象。
    /// </summary>
    public IpcClient()
    {
        JsonIpcProvider = new JsonIpcDirectRoutedProvider(Provider);
    }

    /// <summary>
    /// 连接到 ClassIsland。
    /// </summary>
    public async Task Connect()
    {
        Provider.StartServer();
        JsonIpcProvider.StartServer();
        PeerProxy = await Provider.GetAndConnectToPeerAsync(PipeName);
    }
}