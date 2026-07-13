using System.Windows.Input;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using CommunityToolkit.Mvvm.Input;

namespace ClassIsland.Core.Commands;

/// <summary>
/// Uri 导航相关命令。
/// </summary>
public class UriNavigationCommands
{
    /// <summary>
    /// Uri 导航命令。
    /// </summary>
    public static readonly ICommand UriNavigationCommand = new RelayCommand<string>(url =>
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            IAppHost.TryGetService<IUriNavigationService>()?.NavigateWrapped(uri);
        }
    });
}