using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 表示一个调课安排
/// </summary>
public partial class OrderedScheduleV2 : ObservableObject
{
    /// <summary>
    /// 要调换成的日期，注意只会获取那一天本身的课程，不会再管引用
    /// </summary>
    [ObservableProperty] private DateTime _sourceDate;
    
    /// <summary>
    /// 是否要把这一天的课程设置为空
    /// </summary>
    [ObservableProperty] private bool _setEmpty;
}