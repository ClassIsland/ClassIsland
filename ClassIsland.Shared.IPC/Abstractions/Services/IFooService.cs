using dotnetCampus.Ipc.CompilerServices.Attributes;

namespace ClassIsland.Shared.IPC.Abstractions.Services;

/// <summary>
/// 测试服务。
/// </summary>
[IpcPublic(IgnoresIpcException = true)]
public interface IFooService
{
    /// <summary>
    /// 随便干点啥。
    /// </summary>
    public void DoSomething();
}