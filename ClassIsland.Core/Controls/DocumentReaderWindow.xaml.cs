using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ClassIsland.Core.Helpers;
using ClassIsland.Shared;
using Grpc.Core.Logging;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Core.Controls;

/// <summary>
/// DocumentReaderWindow.xaml 的交互逻辑
/// </summary>
public partial class DocumentReaderWindow : MyWindow
{
    public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(
        nameof(Document), typeof(FlowDocument), typeof(DocumentReaderWindow), new PropertyMetadata(default(FlowDocument)));

    public FlowDocument Document
    {
        get { return (FlowDocument)GetValue(DocumentProperty); }
        set { SetValue(DocumentProperty, value); }
    }

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(Uri), typeof(DocumentReaderWindow), new PropertyMetadata(default(Uri), (o, args) =>
        {
            if (o is not DocumentReaderWindow window)
                return;
            window.LoadDocument();
        }));

    public Uri Source
    {
        get { return (Uri)GetValue(SourceProperty); }
        set { SetValue(SourceProperty, value); }
    }

    public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(
        nameof(IsLoading), typeof(bool), typeof(DocumentReaderWindow), new PropertyMetadata(default(bool)));

    public bool IsLoading
    {
        get { return (bool)GetValue(IsLoadingProperty); }
        private set { SetValue(IsLoadingProperty, value); }
    }

    /// <inheritdoc />
    public DocumentReaderWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private async void LoadDocument()
    {
        try
        {
            IsLoading = true;
            Stream? stream;
            if (!Source.IsAbsoluteUri || Source.Scheme == "pack")
            {
                stream = Application.GetResourceStream(Source)?.Stream;
            }
            else if (Source.Scheme is "http" or "https")
            {
                stream = await new HttpClient().GetStreamAsync(Source);
            }
            else
            {
                throw new InvalidOperationException("只支持从资源或 Http 源加载文档。");
            }

            if (stream == null)
            {
                throw new ArgumentNullException();
            }
            var md = await new StreamReader(stream).ReadToEndAsync();
            Document = MarkdownConvertHelper.ConvertMarkdown(md);
        }
        catch (Exception ex)
        {
            IAppHost.GetService<ILogger<DocumentReaderWindow>>().LogError(ex, "无法加载文档 {}", Source);
            CommonDialog.CommonDialog.ShowError($"无法加载文档 {Source}：{ex.Message}");
        }
        IsLoading = false;
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

    private void ButtonClose_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void DocumentReaderWindow_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }
}