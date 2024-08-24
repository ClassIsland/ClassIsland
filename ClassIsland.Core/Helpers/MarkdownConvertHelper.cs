using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ClassIsland.Core.Commands;
using MdXaml;

namespace ClassIsland.Core.Helpers;

/// <summary>
/// 将 Markdown 转换为 FlowDocument
/// </summary>
public static class MarkdownConvertHelper
{
    /// <summary>
    /// 转换 Markdown 为 FlowDocument
    /// </summary>
    /// <param name="document">要转换的 Markdown 文本</param>
    /// <returns>转换后的 FlowDocument</returns>
    /// <exception cref="InvalidOperationException">当当前<see cref="Application"/>没有初始化时抛出此异常。</exception>
    public static FlowDocument ConvertMarkdown(string document)
    {
        var app = Application.Current;
        if (app == null)
        {
            throw new InvalidOperationException("Application.Current is null!");
        }
        var e = new Markdown
        {
            Heading1Style = app?.FindResource("MarkdownHeadline1Style") as Style,
            Heading2Style = app?.FindResource("MarkdownHeadline2Style") as Style,
            Heading3Style = app?.FindResource("MarkdownHeadline3Style") as Style,
            Heading4Style = app?.FindResource("MarkdownHeadline4Style") as Style,  
            //CodeBlockStyle = (Style)FindResource("MarkdownCodeBlockStyle"),
            //NoteStyle = (Style)FindResource("MarkdownNoteStyle"),
            BlockquoteStyle = app?.FindResource("MarkdownQuoteStyle") as Style,
            ImageStyle = app?.FindResource("MarkdownImageStyle") as Style,
        };
        e.HyperlinkCommand = UriNavigationCommands.UriNavigationCommand;
        var fd = e.Transform(document);
        fd.FontFamily = app?.FindResource("HarmonyOsSans") as FontFamily;
        fd.IsOptimalParagraphEnabled = true;
        return fd;
    }
}