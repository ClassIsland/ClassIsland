using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Models;

namespace ClassIsland.Core.Helpers.UI;

/// <summary>
/// Avalonia 样式选择器解析辅助类
/// </summary>
public static class SelectorHelpers
{
    /// <summary>
    /// 解析指定的样式选择器
    /// </summary>
    /// <param name="expr">样式选择器</param>
    /// <param name="xmlns">XML 命名空间</param>
    /// <returns>解析的样式选择器</returns>
    /// <remarks>
    /// 此方法已预置以下命名空间：
    /// <code>
    /// xmlns="https://github.com/avaloniaui"
    /// xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    /// xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    /// xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    /// xmlns:ci="http://classisland.tech/schemas/xaml/core"
    /// xmlns:fa="clr-namespace:FluentAvalonia.UI.Controls;assembly=FluentAvalonia"
    /// </code>
    /// </remarks>
    public static Selector Parse(string expr, IDictionary<string, string> xmlns)
    {
        var xml = 
            $"""
             <Style 
             xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:ci="http://classisland.tech/schemas/xaml/core"
             xmlns:fa="clr-namespace:FluentAvalonia.UI.Controls;assembly=FluentAvalonia"
             {string.Join(' ', xmlns.Select(x => $"xmlns:{x.Key}=\"{x.Value}\""))}
             Selector="{expr}"/>
             """;
        var style = (Style)AvaloniaRuntimeXamlLoader.Load(xml);
        return style.Selector ?? throw new InvalidOperationException("样式返回了空的选择器");
    }
    
}