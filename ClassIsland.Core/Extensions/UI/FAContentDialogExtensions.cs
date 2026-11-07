using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using ClassIsland.Core.Abstractions.Services.UI;
using ClassIsland.Core.Enums.UI;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Extensions.UI;

/// <summary>
/// <see cref="FAContentDialog"/> 的扩展方法
/// </summary>
public static class FAContentDialogExtensions
{
    /// <summary>
    /// 使用适合的 TopLevel 显示对话框。
    /// </summary>
    /// <param name="dialog">要显示的对话框</param>
    /// <param name="tl">指定的 TopLevel</param>
    public static async Task<FAContentDialogResult> ShowAsyncAuto(this FAContentDialog dialog, TopLevel? tl = null)
    {
        if (tl == null)
        {
            switch (Application.Current?.ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime al:
                {
                    var windows = al.Windows.ToList();
                    foreach (var t in windows.Where(t => t.IsActive))
                    {
                        tl = t;
                        break;
                    }

                    tl ??= al.MainWindow ?? throw new NotSupportedException("No TopLevel root found to parent ContentDialog");
                    break;
                }
                case IActivityApplicationLifetime:
                {
                    var viewHost = IViewHostProvider.Instance.GetViewHost(ViewActivationPreference.Default);
                    if (viewHost is Visual v)
                    {
                        tl = TopLevel.GetTopLevel(v);
                    }

                    break;
                }
                case ISingleViewApplicationLifetime sl:
                    tl = TopLevel.GetTopLevel(sl.MainView);
                    break;
                default:
                    throw new InvalidOperationException("No TopLevel found for ContentDialog and no ApplicationLifetime is set. " +
                                                        "Please either supply a valid ApplicationLifetime or TopLevel to ShowAsync()");
            }
        }
        
        return await dialog.ShowAsync(tl);
    }
}
