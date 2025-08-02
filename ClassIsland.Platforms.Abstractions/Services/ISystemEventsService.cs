namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 系统事件注册服务
/// </summary>
public interface ISystemEventsService
{
    /// <summary>
    /// 系统时间变化时触发
    /// </summary>
    public event EventHandler? TimeChanged;
}