using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using ClassIsland.Controls;
using ClassIsland.ViewModels;

using MdXaml;

using Microsoft.AppCenter.Analytics;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Views;

using PagesDictionary = Dictionary<string, string>;

/// <summary>
/// HelpsWindow.xaml 的交互逻辑
/// </summary>
public partial class HelpsWindow : MyWindow
{
    public static readonly ICommand HyperlinkCommand = new RoutedCommand();

    public static readonly string FooterTemplatePath = "/Assets/Documents/Footer.md";

    public HelpsViewModel ViewModel
    {
        get;
        set;
    } = new();

    public bool IsAutoNavigating
    {
        get;
        set;
    } = false;

    private ILogger<HelpsWindow> Logger { get; }

    public string InitDocumentName { get; set; } = "欢迎";

    public HelpsWindow(ILogger<HelpsWindow> logger)
    {
        Logger = logger;
        DataContext = this;
        InitializeComponent();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        RefreshDocuments();
        UpdateNavigationState();
        ViewModel.NavigationIndex = 0;
        base.OnContentRendered(e);
    }

    private void RefreshDocuments()
    {
        ViewModel.Document = new FlowDocument();
        ViewModel.HelpDocuments.Clear();

        //ViewModel.HelpDocuments.Add("测试", "/Assets/Documents/HelloWorld.md");
        ViewModel.HelpDocuments.Add("欢迎", "/Assets/Documents/Welcome.md");
        ViewModel.HelpDocuments.Add("基本", "/Assets/Documents/Basic.md");
        //ViewModel.HelpDocuments.Add("简略信息", "/Assets/Documents/MiniInfo.md");
        ViewModel.HelpDocuments.Add("提醒", "/Assets/Documents/Notifications.md");
        ViewModel.HelpDocuments.Add("档案设置", "/Assets/Documents/ProfileSettingsPage.md");
        ViewModel.HelpDocuments.Add("课表", "/Assets/Documents/ClassPlan.md");
        ViewModel.HelpDocuments.Add("时间表", "/Assets/Documents/TimeLayout.md");
        ViewModel.HelpDocuments.Add("科目", "/Assets/Documents/Subject.md");
        ViewModel.HelpDocuments.Add("进阶功能", "/Assets/Documents/Advanced.md");
        ViewModel.HelpDocuments.Add("新增功能", "/Assets/Documents/ChangeLog.md");
        //IsAutoNavigating = true;
        ViewModel.SelectedDocumentName = InitDocumentName;
        IsAutoNavigating = true;
        CoreNavigateTo(InitDocumentName);
        IsAutoNavigating = false;
        //IsAutoNavigating = false;
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.SelectedDocumentName != null && !IsAutoNavigating)
        {
            CoreNavigateTo(ViewModel.SelectedDocumentName);
            while (ViewModel.NavigationHistory.Count > ViewModel.NavigationIndex + 1)
            {
                ViewModel.NavigationHistory.RemoveAt(ViewModel.NavigationIndex + 1);
            }
            ViewModel.NavigationHistory.Add(ViewModel.SelectedDocumentName);
            ViewModel.NavigationIndex++;
            UpdateNavigationState();
        }
    }

    public async void CoreNavigateTo(string name)
    {
        Analytics.TrackEvent("浏览帮助文档",
        new PagesDictionary
        {
            { "Name", name }
        });
        //ScrollViewerDocument.ScrollToTop();
        ViewModel.IsLoading = true;
        ViewModel.Document = new FlowDocument();
        //var sw = new Stopwatch();
        //sw.Start();
        await Dispatcher.Yield();
        // 获取相关文档
        var neighbours = new PagesDictionary();
        var docs = ViewModel.HelpDocuments;
        var keys = docs.Keys.ToList();
        var index = keys.IndexOf(name);
        if (index > 0)
        {
            neighbours.Add($"上一篇：{keys[index - 1]}", keys[index - 1]);
        }

        if (index < keys.Count - 1)
        {
            neighbours.Add($"下一篇：{keys[index + 1]}", keys[index + 1]);
        }
        ConvertMarkdown(docs[name], neighbours, new PagesDictionary());
        ViewModel.SelectedDocumentName = name;
        await Dispatcher.Yield();
        //Console.WriteLine(sw.Elapsed.ToString());
        RootScrollViewer.ScrollToTop();
        ViewModel.IsLoading = false;
    }

    private void ConvertMarkdown(string path, PagesDictionary neighbourPages, PagesDictionary relatedPages)
    {
        var r = Application.GetResourceStream(new Uri(path, UriKind.RelativeOrAbsolute))?.Stream;
        var footerStream = Application.GetResourceStream(new Uri(FooterTemplatePath, UriKind.RelativeOrAbsolute))?.Stream;
        if (r == null || footerStream == null)
        {
            return;
        }
        var md = new StreamReader(r).ReadToEnd();
        var footer = new StreamReader(footerStream).ReadToEnd();

        var t = md + "\n" + 
                string.Format(footer, string.Join('\n', from i in neighbourPages select $"- [{i.Key}]({i.Value})"));
        
        var e = new Markdown()
        {
            Heading1Style = (Style)FindResource("MarkdownHeadline1Style"),
            Heading2Style = (Style)FindResource("MarkdownHeadline2Style"),
            Heading3Style = (Style)FindResource("MarkdownHeadline3Style"),
            Heading4Style = (Style)FindResource("MarkdownHeadline4Style"),
            //CodeBlockStyle = (Style)FindResource("MarkdownCodeBlockStyle"),
            //NoteStyle = (Style)FindResource("MarkdownNoteStyle"),
            BlockquoteStyle = (Style)FindResource("MarkdownQuoteStyle"),
            ImageStyle = (Style)FindResource("MarkdownImageStyle"),
        };
        e.HyperlinkCommand = HyperlinkCommand;
        var fd = e.Transform(t);
        fd.FontFamily = (FontFamily)FindResource("HarmonyOsSans");
        fd.IsOptimalParagraphEnabled = true;
        //fd.MaxPageWidth = 850;
        ViewModel.Document = fd;
    }

    private void ButtonRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshDocuments();
    }

    private void HelpsWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        ViewModel.IsOpened = false;
    }

    private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!e.Handled)
        {
            // ListView拦截鼠标滚轮事件
            e.Handled = true;

            // 激发一个鼠标滚轮事件，冒泡给外层ListView接收到
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            var parent = ((System.Windows.Controls.Control)sender).Parent as UIElement;
            if (parent != null)
            {
                parent.RaiseEvent(eventArg);
            }
        }
    }

    private void ButtonBack_OnClick(object sender, RoutedEventArgs e)
    {
        IsAutoNavigating = true;
        Debug.WriteLine($"{ViewModel.NavigationIndex} {string.Join(' ', ViewModel.NavigationHistory)}");
        if (ViewModel.NavigationIndex <= 0)
        {
            return;
        }
        CoreNavigateTo(ViewModel.NavigationHistory[ViewModel.NavigationIndex - 1]);
        ViewModel.NavigationIndex--;
        IsAutoNavigating = false;
        UpdateNavigationState();
    }

    private void ButtonForward_OnClick(object sender, RoutedEventArgs e)
    {
        IsAutoNavigating = true;
        Debug.WriteLine($"{ViewModel.NavigationIndex} {string.Join(' ', ViewModel.NavigationHistory)}");
        if (ViewModel.NavigationIndex + 1 >= ViewModel.NavigationHistory.Count)
        {
            return;
        }
        CoreNavigateTo(ViewModel.NavigationHistory[ViewModel.NavigationIndex + 1]);
        ViewModel.NavigationIndex++;
        IsAutoNavigating = false;
        UpdateNavigationState();
    }

    private void UpdateNavigationState()
    {
        ViewModel.CanBack = ViewModel.NavigationIndex > 0;
        ViewModel.CanForward = ViewModel.NavigationIndex + 1 < ViewModel.NavigationHistory.Count;
    }

    private void CommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        var p = e.Parameter;
        Console.WriteLine( p.ToString() );
        if (e.Parameter is not string s)
            return;
        Uri uri;
        try
        {
            uri = new Uri(s);
        }
        catch (Exception ex)
        {
            CoreNavigateTo(s);
            return;
        }

        if (uri.Scheme != "ci")
        {
            try
            {
                Process.Start(new ProcessStartInfo(uri.ToString())
                {
                    UseShellExecute = true
                });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Unable to open external link.");
            }
            return;
        }
        if (uri.Host != "app" || uri.Segments.Length <= 1)
            return;
        var mw = App.GetService<MainWindow>();
        switch (uri.Segments[1])
        {
            case "settings/":
                mw.OpenSettingsWindow();
                mw.SettingsWindow.OpenUri(uri);
                break;
            case "profile":
                mw.OpenProfileSettingsWindow();
                break;
        }
    }
}