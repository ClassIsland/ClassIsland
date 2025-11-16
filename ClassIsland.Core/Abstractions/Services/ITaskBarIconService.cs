using System.Collections.ObjectModel;
using Avalonia.Controls;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 任务栏图标服务
/// </summary>
public interface ITaskBarIconService
{
    /// <summary>
    /// 任务栏图标实例
    /// </summary>
    TrayIcon MainTaskBarIcon { get; }
    
    /// <summary>
    /// 主菜单更多选项的内容
    /// </summary>
    IList<NativeMenuItemBase> MoreOptionsMenuItems { get; }
    
    /// <summary>
    /// 用于承载更多选项内容的 <see cref="NativeMenu"/>
    /// </summary>
    internal NativeMenu? MoreOptionsMenu { get; set; }
}