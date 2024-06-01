using System.ComponentModel;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 应用挂起检测服务
/// </summary>
public interface IHangService : INotifyPropertyChanged
{
    /// <summary>
    /// 应用是否挂起
    /// </summary>
    public bool IsHang { get; set; }

    /// <summary>
    /// 是否正在检查挂起
    /// </summary>
    public bool IsChecking { get; set; }

    /// <summary>
    /// 假定应用挂起，并立即检查是否挂起。
    /// </summary>
    public void AssumeHang();
}