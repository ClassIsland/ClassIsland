using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using dotnetCampus.Ipc.Pipes;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 跨进程通信服务。
/// </summary>
public interface IIpcService
{
    /// <summary>
    /// 跨进程通信提供方
    /// </summary>
    public IpcProvider IpcProvider { get; }
    
    /// <summary>
    /// JSON 路由通信提供方
    /// </summary>
    public JsonIpcDirectRoutedProvider JsonRoutedProvider { get; }

    /// <summary>
    /// 向所有连接的对方广播消息。
    /// </summary>
    /// <param name="id">消息 id</param>
    public Task BroadcastNotificationAsync(string id);

    /// <summary>
    /// 向所有连接的对方广播消息。
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="id">消息 id</param>
    /// <param name="obj">参数对象。</param>
    public Task BroadcastNotificationAsync<T>(string id, T obj) where T : class;
}