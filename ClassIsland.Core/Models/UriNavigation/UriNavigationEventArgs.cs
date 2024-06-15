namespace ClassIsland.Core.Models.UriNavigation;

/// <summary>
/// Uri 导航事件参数。
/// </summary>
public class UriNavigationEventArgs
{
    internal UriNavigationEventArgs(Uri uri, IReadOnlyList<string> childrenPathPatterns)
    {
        Uri = uri;
        ChildrenPathPatterns = childrenPathPatterns;
    }

    /// <summary>
    /// 导航时传入的完整 uri。
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// 导航时在当前注册的 uri 的子路径。
    /// </summary>
    public IReadOnlyList<string> ChildrenPathPatterns { get; }
}