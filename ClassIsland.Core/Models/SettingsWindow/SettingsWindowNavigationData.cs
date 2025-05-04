using System.Web;

namespace ClassIsland.Core.Models.SettingsWindow;

/// <summary>
/// 代表设置窗口页面的导航附加信息。
/// </summary>
public class SettingsWindowNavigationData
{
    internal SettingsWindowNavigationData(bool isNavigateFromSettingsWindow, bool isNavigateFromUri, Uri? navigateUri, bool keepHistory, object? transaction, object? span)
    {
        IsNavigateFromSettingsWindow = isNavigateFromSettingsWindow;
        IsNavigateFromUri = isNavigateFromUri;
        NavigateUri = navigateUri;
        KeepHistory = keepHistory;
        Transaction = transaction;
        Span = span;
    }

    /// <summary>
    /// 此页面是否从设置页面导航。
    /// </summary>
    /// <remarks>
    /// 如果这个页面使用 Uri 导航，此属性也为 true
    /// </remarks>
    public bool IsNavigateFromSettingsWindow { get; }

    /// <summary>
    /// 此页面是否从 Uri 导航
    /// </summary>
    public bool IsNavigateFromUri { get; }

    /// <summary>
    /// 导航到此页面的完整 Uri
    /// </summary>
    /// <remarks>
    /// 仅当<see cref="IsNavigateFromUri"/>为 true 时，此属性不为空。
    /// </remarks>
    public Uri? NavigateUri { get; }

    /// <summary>
    /// 导航时是否需要保留历史记录
    /// </summary>
    public bool KeepHistory { get; }

    public object? Transaction { get; }
    public object? Span { get; }
}