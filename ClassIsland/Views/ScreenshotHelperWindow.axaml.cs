using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ClassIsland.Core;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Shared;
using ClassIsland.ViewModels;

namespace ClassIsland.Views;

public partial class ScreenshotHelperWindow : MyWindow
{
    public ScreenshotHelperViewModel ViewModel { get; } = IAppHost.GetService<ScreenshotHelperViewModel>();
    
    public ScreenshotHelperWindow()
    {
        DataContext = this;
        InitializeComponent();
        RefreshWindows();
    }

    private void RefreshWindows()
    {
        ViewModel.Windows =
            [..AppBase.Current.DesktopLifetime!.Windows.Select(x => new ScreenshotHelperViewModel.WindowInfo(x))];
        
    }

    private void ButtonRefresh_OnClick(object? sender, RoutedEventArgs e)
    {
        RefreshWindows();
    }

    private void ButtonScreenshot_OnClick(object? sender, RoutedEventArgs e)
    {
        var visual = ViewModel.SelectedWindow?.Window;
        if (visual == null)
        {
            this.ShowWarningToast("请先选择一个有效窗口。");
            return;
        }

        var filePath = Path.Combine(ViewModel.ImageBasePath, Guid.NewGuid().ToString() + ".png");
        var width = (int)(visual.Bounds.Width * ViewModel.Scale);
        var height = (int)(visual.Bounds.Height * ViewModel.Scale);
 
        var visualBrush = new VisualBrush(visual)
        {
            
        };
 
        var renderTargetBitmap = new RenderTargetBitmap(new PixelSize(width, height));
        using var canvas = renderTargetBitmap.CreateDrawingContext(false);
 
        canvas.DrawRectangle(visualBrush, null, new Rect(0, 0, width, height));
        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        }
        renderTargetBitmap.Save(filePath);
        this.ShowSuccessToast(filePath);
    }
}