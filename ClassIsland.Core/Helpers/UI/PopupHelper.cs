using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace ClassIsland.Core.Helpers.UI;

/// <summary>
/// Popup 相关助手方法
/// </summary>
public class PopupHelper
{
    /// <summary>
    /// 请求禁用全部 Popup 时触发。
    /// </summary>
    public static event EventHandler? DisablePopupsRequested;
    
    /// <summary>
    /// 请求恢复全部 Popup 时触发。
    /// </summary>
    public static event EventHandler? RestorePopupsRequested;
    
    /// <summary>
    /// 禁用全部 Popup
    /// </summary>
    public static void DisableAllPopups()
    {
        DisablePopupsRequested?.Invoke(null, EventArgs.Empty);
    }
    
    /// <summary>
    /// 恢复全部 Popup
    /// </summary>
    public static void RestoreAllPopups()
    {
        RestorePopupsRequested?.Invoke(null, EventArgs.Empty);
    }
}