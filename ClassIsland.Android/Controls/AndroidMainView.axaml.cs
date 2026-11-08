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
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.ViewModels;

namespace ClassIsland.Android.Controls;

public partial class AndroidMainView : UserControl, INotificationRenderSizeHost, IMainWindowNotificationVisibilityHost
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

    internal Func<double, IDisposable>? ReserveHeight { get; set; }

    public AndroidMainView()
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
        ComponentPresenter.SetIsMainWindowLoaded(this, false);
        XamlThemeService.SetResourceHost(null);
        ReserveHeight = null;
        _topmostLocks.Clear();
        ViewModel.IsNotificationWindowExplicitShowed = false;
    }

    void IMainWindowNotificationVisibilityHost.AcquireTopmostLock(object token)
    {
        _topmostLocks.Add(token);
        ViewModel.IsNotificationWindowExplicitShowed = true;
    }

    void IMainWindowNotificationVisibilityHost.ReleaseTopmostLock(object token)
    {
        _topmostLocks.Remove(token);
        ViewModel.IsNotificationWindowExplicitShowed = _topmostLocks.Count > 0;
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
        // AvaloniaView prepares the template before Content is assigned; changing the fallback brush
        // afterward does not refresh the background already set on the fallback border.
        var fallback = root.GetTemplateChildren().OfType<Border>()
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

    IDisposable INotificationRenderSizeHost.ReserveRenderHeight(double additionalHeight) =>
        ReserveHeight?.Invoke(additionalHeight) ?? System.Reactive.Disposables.Disposable.Empty;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        InitializeHost();
    }
}
