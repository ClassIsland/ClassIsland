using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Notification.Templates;

/// <summary>
/// 代表滚动文本模板的数据。
/// </summary>
public partial class RollingTextTemplateData : ObservableObject
{
    /// <summary>
    /// 要显示的文本
    /// </summary>
    [ObservableProperty] private string _text = "";

    /// <summary>
    /// 滚动时长
    /// </summary>
    [ObservableProperty] private TimeSpan _duration = TimeSpan.Zero;

    /// <summary>
    /// 滚动重复次数
    /// </summary>
    [ObservableProperty] private int _repeatCount = 2;
}