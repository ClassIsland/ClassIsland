using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClassIsland.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.ViewModels;

namespace ClassIsland.iOS.Controls;

/// <summary>
/// iOS HUD 悬浮主界面岛：在 HUD 宿主进程的全屏透明窗口顶部/底部渲染共享的主界面组件行。
/// </summary>
public partial class IosMainWindowIslandView : UserControl, INotificationRenderSizeHost,
    IMainWindowNotificationVisibilityHost
{
    public MainViewModel ViewModel { get; } = new();
    public SettingsService SettingsService { get; } = IAppHost.GetService<SettingsService>();
    public IComponentsService ComponentsService { get; } = IAppHost.GetService<IComponentsService>();
    public IProfileService ProfileService { get; } = IAppHost.GetService<IProfileService>();
    public ILessonsService LessonsService { get; } = IAppHost.GetService<ILessonsService>();

    private IXamlThemeService XamlThemeService { get; } = IAppHost.GetService<IXamlThemeService>();
    private INotifyPropertyChanged? ProfileChanges => ProfileService as INotifyPropertyChanged;
    private bool _isInitialized;
    private TopLevel? _hostRoot;
    private readonly HashSet<object> _topmostLocks = [];

    public IosMainWindowIslandView()
    {
        ViewModel.Settings = SettingsService.Settings;
        ViewModel.Profile = ProfileService.Profile;
        DataContext = this;
        InitializeComponent();
        RootMainWindowLinesItemsControl.ContainerIndexChanged += LinesOnContainerIndexChanged;
    }

    internal void InitializeHost()
    {
        if (_isInitialized)
        {
            return;
        }
        _isInitialized = true;
        if (TopLevel.GetTopLevel(this) is { } root)
        {
            _hostRoot = root;
            root.DataContext = this;
            root.Background = Brushes.Transparent;
            root.TransparencyBackgroundFallback = Brushes.Transparent;
            root.TemplateApplied += RootOnTemplateApplied;
            ClearTransparencyFallback(root);
        }
        XamlThemeService.SetResourceHost(ResourceLoaderBorder);
        XamlThemeService.LoadAllThemes();
        UpdateAppearance();
        if (ProfileChanges != null)
        {
            ProfileChanges.PropertyChanged += ProfileServiceOnPropertyChanged;
        }
        SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
        UpdateVisibility();
        ComponentPresenter.SetIsMainWindowLoaded(this, true);
    }

    internal void UpdateAppearance()
    {
        ViewModel.Settings = SettingsService.Settings;
        var settings = ViewModel.Settings;
        TextElement.SetFontFamily(this, FontFamily.Parse(settings.MainWindowFont));
        TextElement.SetFontWeight(this, (FontWeight)settings.MainWindowFontWeight2);
        ResourceLoaderBorder.Resources[nameof(settings.MainWindowSecondaryFontSize)] = settings.MainWindowSecondaryFontSize;
        ResourceLoaderBorder.Resources[nameof(settings.MainWindowBodyFontSize)] = settings.MainWindowBodyFontSize;
        ResourceLoaderBorder.Resources[nameof(settings.MainWindowEmphasizedFontSize)] = settings.MainWindowEmphasizedFontSize;
        ResourceLoaderBorder.Resources[nameof(settings.MainWindowLargeFontSize)] = settings.MainWindowLargeFontSize;
        ControlColorHelper.SetControlForegroundColor(ResourceLoaderBorder, settings.CustomForegroundColor,
            settings.IsCustomForegroundColorEnabled);
        Color? primary = settings.ColorSource switch
        {
            0 => settings.PrimaryColor,
            1 or 3 => settings.SelectedPlatte,
            2 => null,
            _ => Colors.DodgerBlue
        };
        IAppHost.GetService<IThemeService>().SetTheme(settings.Theme, primary);
    }

    internal void ReleaseHost()
    {
        if (!_isInitialized)
        {
            return;
        }
        _isInitialized = false;
        if (_hostRoot != null)
        {
            _hostRoot.TemplateApplied -= RootOnTemplateApplied;
            _hostRoot = null;
        }
        if (ProfileChanges != null)
        {
            ProfileChanges.PropertyChanged -= ProfileServiceOnPropertyChanged;
        }
        SettingsService.Settings.PropertyChanged -= SettingsOnPropertyChanged;
        ComponentPresenter.SetIsMainWindowLoaded(this, false);
        XamlThemeService.SetResourceHost(null);
        _topmostLocks.Clear();
        ViewModel.IsNotificationWindowExplicitShowed = false;
    }

    void IMainWindowNotificationVisibilityHost.AcquireTopmostLock(object token)
    {
        _topmostLocks.Add(token);
        ViewModel.IsNotificationWindowExplicitShowed = true;
        UpdateVisibility();
    }

    void IMainWindowNotificationVisibilityHost.ReleaseTopmostLock(object token)
    {
        _topmostLocks.Remove(token);
        ViewModel.IsNotificationWindowExplicitShowed = _topmostLocks.Count > 0;
        UpdateVisibility();
    }

    /// <summary>
    /// 与 Android 悬浮主界面一致：隐藏主界面时保持视图树存活，提醒置顶时强制显示。
    /// 隐藏规则（按上课状态等）暂未在 HUD 宿主中实现。
    /// </summary>
    private void UpdateVisibility()
    {
        IsVisible = SettingsService.Settings.IsMainWindowVisible ||
                    ViewModel.IsNotificationWindowExplicitShowed;
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.IsMainWindowVisible))
        {
            UpdateVisibility();
        }
    }

    private static void RootOnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (sender is TopLevel root)
        {
            ClearTransparencyFallback(root);
        }
    }

    private static void ClearTransparencyFallback(TopLevel root)
    {
        // 嵌入式顶层会在内容挂载前按默认回退刷子绘制背景，之后只改属性不会刷新已经生成的回退边框，
        // 因此模板应用后还要显式清空，避免悬浮窗口带上不透明底色。
        // Avalonia 12 已弃用 GetTemplateChildren，GetTemplateDescendants 是等价的替代实现。
        var fallback = root.GetTemplateDescendants().OfType<Border>()
            .FirstOrDefault(x => x.Name == "PART_TransparencyFallback");
        if (fallback != null)
        {
            fallback.Background = Brushes.Transparent;
        }
    }

    private void ProfileServiceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_isInitialized)
            {
                ViewModel.Profile = ProfileService.Profile;
            }
        });
    }

    private void Line_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is MainWindowLine line)
        {
            line.LineNumber = ComponentsService.CurrentComponents.Lines.IndexOf(line.Settings);
        }
    }

    private void LinesOnContainerIndexChanged(object? sender, ContainerIndexChangedEventArgs e)
    {
        var line = e.Container.GetVisualDescendants().OfType<MainWindowLine>().FirstOrDefault();
        if (line != null)
        {
            line.LineNumber = e.NewIndex;
        }
    }

    // HUD 窗口本身是全屏的，提醒渲染不需要额外预留高度。
    IDisposable INotificationRenderSizeHost.ReserveRenderHeight(double additionalHeight) =>
        System.Reactive.Disposables.Disposable.Empty;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        InitializeHost();
    }
}
