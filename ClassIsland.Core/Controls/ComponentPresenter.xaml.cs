using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Components;
using ClassIsland.Shared;

namespace ClassIsland.Core.Controls;

/// <summary>
/// ComponentPresenter.xaml 的交互逻辑
/// </summary>
public partial class ComponentPresenter : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
        nameof(Settings), typeof(ComponentSettings), typeof(ComponentPresenter), new PropertyMetadata(default(ComponentSettings), PropertyChangedCallback));

    public static readonly DependencyProperty IsPresentingSettingsProperty = DependencyProperty.Register(
        nameof(IsPresentingSettings), typeof(bool), typeof(ComponentPresenter), new PropertyMetadata(false));

    public bool IsPresentingSettings
    {
        get { return (bool)GetValue(IsPresentingSettingsProperty); }
        set { SetValue(IsPresentingSettingsProperty, value); }
    }

    public static readonly DependencyProperty HideOnRuleProperty = DependencyProperty.Register(
        nameof(HideOnRule), typeof(bool), typeof(ComponentPresenter), new PropertyMetadata(false, (o, args) =>
        {
            if (o is ComponentPresenter control)
            {
                control.UpdateWindowRuleState();
            }
        }));

    public bool HideOnRule
    {
        get { return (bool)GetValue(HideOnRuleProperty); }
        set { SetValue(HideOnRuleProperty, value); }
    }

    public static readonly DependencyProperty HidingRulesProperty = DependencyProperty.Register(
        nameof(HidingRules), typeof(Models.Ruleset.Ruleset), typeof(ComponentPresenter), new PropertyMetadata(default(Models.Ruleset.Ruleset),
            (o, args) =>
            {
                if (o is ComponentPresenter control)
                {
                    control.CheckHideRule();
                }
            }));

    public Models.Ruleset.Ruleset? HidingRules
    {
        get { return (Models.Ruleset.Ruleset)GetValue(HidingRulesProperty); }
        set { SetValue(HidingRulesProperty, value); }
    }

    public static readonly DependencyProperty IsOnMainWindowProperty = DependencyProperty.Register(
        nameof(IsOnMainWindow), typeof(bool), typeof(ComponentPresenter), new PropertyMetadata(default(bool),
            (o, args) =>
            {
                if (o is ComponentPresenter control)
                {
                    control.UpdateTheme();
                }
            }));

    public bool IsOnMainWindow
    {
        get { return (bool)GetValue(IsOnMainWindowProperty); }
        set { SetValue(IsOnMainWindowProperty, value); }
    }

    public static readonly DependencyProperty IsRootComponentProperty = DependencyProperty.Register(
        nameof(IsRootComponent), typeof(bool), typeof(ComponentPresenter), new PropertyMetadata(default(bool)));

    public bool IsRootComponent
    {
        get { return (bool)GetValue(IsRootComponentProperty); }
        set { SetValue(IsRootComponentProperty, value); }
    }

    private bool _isAllComponentsHid = false;

    public static readonly RoutedEvent ComponentVisibilityChangedEvent = EventManager.RegisterRoutedEvent(
        name: "ComponentVisibilityChanged",
        routingStrategy: RoutingStrategy.Bubble,
        handlerType: typeof(RoutedEventHandler),
        ownerType: typeof(ComponentPresenter));

    // Provide CLR accessors for adding and removing an event handler.
    public event RoutedEventHandler ComponentVisibilityChanged
    {
        add { AddHandler(ComponentVisibilityChangedEvent, value); }
        remove { RemoveHandler(ComponentVisibilityChangedEvent, value); }
    }

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ComponentPresenter p)
        {
            p.UpdateContent(e.OldValue as ComponentSettings);
        }
    }

    private IRulesetService RulesetService { get; } = IAppHost.GetService<IRulesetService>();

    private object? _presentingContent;

    public ComponentSettings? Settings
    {
        get { return (ComponentSettings)GetValue(SettingsProperty); }
        set { SetValue(SettingsProperty, value); }
    }

    private void UpdateContent(ComponentSettings? oldSettings)
    {
        if (oldSettings != null)
        {
            oldSettings.PropertyChanged -= SettingsOnPropertyChanged;
            if (oldSettings.Children != null)
            {
                oldSettings.Children.CollectionChanged -= ChildrenOnCollectionChanged;
            }
        }
        RaiseEvent(new RoutedEventArgs(ComponentVisibilityChangedEvent));
        if (Settings == null)
            return;
        Settings.PropertyChanged += SettingsOnPropertyChanged;
        if (Settings.Children != null)
        {
            Settings.Children.CollectionChanged += ChildrenOnCollectionChanged;
        }
        var content = IAppHost.GetService<IComponentsService>().GetComponent(Settings, IsPresentingSettings);
        // 理论上展示的内容的数据上下文应为MainWindow，这里不便用前端xaml绑定，故在后台设置。
        if (content != null && IsOnMainWindow)
        {
            content.DataContext = Window.GetWindow(this);
        }

        PresentingContent = content;
        UpdateTheme();
    }

    private void ChildrenOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(ComponentVisibilityChangedEvent));
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateTheme();
        if (e.PropertyName == nameof(Settings.RelativeLineNumber))
        {
            RaiseEvent(new RoutedEventArgs(ComponentVisibilityChangedEvent));
        }
    }

    private void UpdateTheme()
    {
        if (Settings == null || !IsOnMainWindow)
            return;
        if (Settings.IsResourceOverridingEnabled)
        {
            Resources[nameof(Settings.MainWindowSecondaryFontSize)] = Settings.MainWindowSecondaryFontSize;
            Resources[nameof(Settings.MainWindowBodyFontSize)] = Settings.MainWindowBodyFontSize;
            Resources[nameof(Settings.MainWindowEmphasizedFontSize)] = Settings.MainWindowEmphasizedFontSize;
            Resources[nameof(Settings.MainWindowLargeFontSize)] = Settings.MainWindowLargeFontSize;
        }
        else
        {
            foreach (var key in (string[])
                     [
                         nameof(Settings.MainWindowSecondaryFontSize), nameof(Settings.MainWindowBodyFontSize),
                         nameof(Settings.MainWindowEmphasizedFontSize), nameof(Settings.MainWindowLargeFontSize)
                     ])
            {
                if (Resources.Contains(key))
                {
                    Resources.Remove(key);
                }
            }
        }

        if (Settings.IsCustomForegroundColorEnabled)
        {
            var brush = new SolidColorBrush(Settings.ForegroundColor);
            SetValue(Control.ForegroundProperty, brush);
            SetValue(TextElement.ForegroundProperty, brush);
            Resources["MaterialDesignBody"] = brush;
        }
        else
        {
            if (Resources.Contains("MaterialDesignBody"))
            {
                Resources.Remove("MaterialDesignBody");
            }
            SetValue(Control.ForegroundProperty, DependencyProperty.UnsetValue);
            SetValue(TextElement.ForegroundProperty, DependencyProperty.UnsetValue);
        }
    }

    public object? PresentingContent
    {
        get => _presentingContent;
        set
        {
            if (Equals(value, _presentingContent)) return;
            _presentingContent = value;
            OnPropertyChanged();
        }
    }

    public ComponentPresenter()
    {
        InitializeComponent();
    }

    private void UpdateWindowRuleState()
    {
        if (HideOnRule)
        {
            CheckHideRule();
            RulesetService.StatusUpdated += RulesetServiceOnStatusUpdated;
        }
        else
        {
            RulesetService.StatusUpdated -= RulesetServiceOnStatusUpdated;
            Visibility = Visibility.Visible;
        }
        UpdateComponentHidState();
    }

    private void RulesetServiceOnStatusUpdated(object? sender, EventArgs e)
    {
        CheckHideRule();
        UpdateComponentHidState();
    }

    private void CheckHideRule()
    {
        if (!HideOnRule)
        {
            return;
        }
        if (HidingRules != null && RulesetService.IsRulesetSatisfied(HidingRules))
        {
            Visibility = Visibility.Collapsed;
        }
        else
        {
            Visibility = Visibility.Visible;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void MainContentPresenter_OnComponentVisibilityChanged(object sender, RoutedEventArgs e)
    {
        e.Handled = true;  // 不需要继续向上冒泡
        UpdateComponentHidState();
    }

    private void UpdateComponentHidState()
    {
        var visibleStatePrev = Settings?.IsVisible;
        _isAllComponentsHid = Settings?.Children != null && Settings.Children.FirstOrDefault(x => x.IsVisible) == null;
        if (Settings != null) Settings.IsVisible = Visibility == Visibility.Visible && !_isAllComponentsHid;
        if (Settings?.IsVisible != visibleStatePrev)
        {
            RaiseEvent(new RoutedEventArgs(ComponentVisibilityChangedEvent));
        }
    }

    private void ComponentPresenter_OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateComponentHidState();
    }
}