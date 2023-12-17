using ClassIsland.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
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
using ClassIsland.Services;

namespace ClassIsland.Views;

/// <summary>
/// WallpaperPreviewWindow.xaml 的交互逻辑
/// </summary>
public partial class WallpaperPreviewWindow : MyWindow
{
    public static readonly DependencyProperty PreviewImageProperty = DependencyProperty.Register(
        nameof(PreviewImage), typeof(BitmapImage), typeof(WallpaperPreviewWindow), new PropertyMetadata(default(BitmapImage)));

    public BitmapImage PreviewImage
    {
        get { return (BitmapImage)GetValue(PreviewImageProperty); }
        set { SetValue(PreviewImageProperty, value); }
    }

    public WallpaperPickingService WallpaperPickingService { get; }

    public WallpaperPreviewWindow(WallpaperPickingService wallpaperPickingService)
    {
        InitializeComponent();
        WallpaperPickingService = wallpaperPickingService;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        DataContext = this;
    }

    private async void ButtonRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        await WallpaperPickingService.GetWallpaperAsync();
    }
}