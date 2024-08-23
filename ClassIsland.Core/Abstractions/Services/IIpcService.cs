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
}