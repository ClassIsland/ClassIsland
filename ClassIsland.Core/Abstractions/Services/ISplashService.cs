using System.ComponentModel;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 启动屏幕服务，用于控制启动屏幕。
/// </summary>
public interface ISplashService : INotifyPropertyChanged, INotifyPropertyChanging
{
    /// <summary>
    /// 启动屏幕状态文字
    /// </summary>
    string SplashStatus { get; set; }
    
    /// <summary>
    /// 当前启动进度
    /// </summary>
    double CurrentProgress { get; set; }
    /// <summary>
    /// 启动屏幕进度改变事件
    /// </summary>
    event EventHandler<double>? ProgressChanged;
    /// <summary>
    /// 启动屏幕结束事件
    /// </summary>
    event EventHandler? SplashEnded;
    internal void EndSplash();
    /// <summary>
    /// 重置启动屏幕文字。
    /// </summary>
    void ResetSplashText();
}