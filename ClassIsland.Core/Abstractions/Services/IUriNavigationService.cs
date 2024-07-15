using ClassIsland.Core.Models.UriNavigation;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// Uri导航服务，用于在ClassIsland内部和外部通过uri进行导航。
/// </summary>
public interface IUriNavigationService
{
    /// <summary>
    /// ClassIsland 内部的 uri 协议名
    /// </summary>
    public static string UriScheme { get; } = "classisland";

    public static string UriDomainApp { get; } = "app";

    public static string UriDomainPlugins { get; } = "plugins";


    internal void HandleNavigation(string domain, string path, Action<UriNavigationEventArgs> onNavigated);

    internal void HandleAppNavigation(string path, Action<UriNavigationEventArgs> onNavigated);

    public void HandlePluginsNavigation(string path, Action<UriNavigationEventArgs> onNavigated);

    public void Navigate(Uri uri);

    public void NavigateWrapped(Uri uri);
    public void NavigateWrapped(Uri uri, out Exception? exception);
}