using Avalonia;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 一些常用的 TaskDialog 
/// </summary>
public static class CommonTaskDialogs
{
    private static Window PhonyXamlRootWindow { get; } = new();
    
    /// <summary>
    /// 显示基本提示框
    /// </summary>
    /// <param name="header">对话框头</param>
    /// <param name="content">要显示的内容</param>
    /// <param name="xamlRoot">XAML 根元素</param>
    public static async Task<object?> ShowDialog(string header, string content, Visual? xamlRoot = null)
    {
        var dialog = new TaskDialog()
        {
            Content = content,
            Header = header,
            Buttons =
            {
                new TaskDialogButton("确定", true)
                {
                    IsDefault = true,
                }
            },
            XamlRoot = xamlRoot ?? AppBase.Current.GetRootWindow()
        };
        
        return await dialog.ShowAsync();
    }
}