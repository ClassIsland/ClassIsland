using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using ClassIsland.Shared;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Theming;
using FluentAvalonia.UI.Windowing;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 通用窗口基类
/// </summary>
public class MyWindow : AppWindow
{
    private bool _isAdornerAdded;

    /// <summary>
    /// 是否显示开源警告水印
    /// </summary>
    public static bool ShowOssWatermark { get; internal set; } = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MyWindow()
    {
        try
        {
            IAppHost.GetService<IHangService>().AssumeHang();
        }
        catch
        {
            // ignored
        }
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if ((AppBase.Current.IsDevelopmentBuild || ShowOssWatermark)&& Content is Control element && !_isAdornerAdded)
        {
            var layer = AdornerLayer.GetAdornerLayer(element);
            var adorner = new DevelopmentBuildAdorner(AppBase.Current.IsDevelopmentBuild, ShowOssWatermark);
            layer?.Children.Add(adorner);
            AdornerLayer.SetAdornedElement(adorner, this);
            _isAdornerAdded = true;
        }
    }

}