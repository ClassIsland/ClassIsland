using System;
using System.Windows;
using Avalonia.Media;
using Avalonia.Styling;
using ClassIsland.Core.Commands;
using Markdown.Avalonia;
using Markdown.Avalonia.SyntaxHigh;

namespace ClassIsland.Core.Helpers;

/// <summary>
/// 将 Markdown 转换为 FlowDocument
/// </summary>
public static class MarkdownConvertHelper
{
    private static Markdown.Avalonia.Markdown? _engine;
    
    /// <summary>
    /// 获取 Markdown 引擎。
    /// </summary>
    public static Markdown.Avalonia.Markdown Engine {
        get
        {
            _engine ??= CreateEngine();
            return _engine;
        }
    }


    private static Markdown.Avalonia.Markdown CreateEngine()
    {
        var e = new Markdown.Avalonia.Markdown
        {
            HyperlinkCommand = UriNavigationCommands.UriNavigationCommand
        };
        return e;
    }
}
