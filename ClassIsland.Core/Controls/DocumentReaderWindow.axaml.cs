using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Shared;
using Grpc.Core.Logging;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Core.Controls;

/// <summary>
/// DocumentReaderWindow.xaml 的交互逻辑
/// </summary>
public partial class DocumentReaderWindow : MyWindow
{
    public static readonly StyledProperty<Uri> SourceProperty = AvaloniaProperty.Register<DocumentReaderWindow, Uri>(
        nameof(Source));

    public Uri Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly StyledProperty<bool> IsLoadingProperty = AvaloniaProperty.Register<DocumentReaderWindow, bool>(
        nameof(IsLoading));

    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    private string _markdown;

    public static readonly DirectProperty<DocumentReaderWindow, string> MarkdownProperty = AvaloniaProperty.RegisterDirect<DocumentReaderWindow, string>(
        nameof(Markdown), o => o.Markdown, (o, v) => o.Markdown = v);

    public string Markdown
    {
        get => _markdown;
        set => SetAndRaise(MarkdownProperty, ref _markdown, value);
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
            if (Source.Scheme == "avares")
            {
                stream = AssetLoader.Open(Source);
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
            Markdown = md;
        }
        catch (Exception ex)
        {
            IAppHost.GetService<ILogger<DocumentReaderWindow>>().LogError(ex, "无法加载文档 {}", Source);
            this.ShowErrorToast($"无法加载文档 {Source}", ex);
        }
        IsLoading = false;
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

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        LoadDocument();
    }
}
