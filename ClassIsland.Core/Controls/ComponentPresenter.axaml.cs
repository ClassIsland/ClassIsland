using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Reactive;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Components;
using ClassIsland.Shared;

namespace ClassIsland.Core.Controls;

/// <summary>
/// ComponentPresenter.xaml 的交互逻辑
/// </summary>
public partial class ComponentPresenter : UserControl, INotifyPropertyChanged
{
    public static readonly StyledProperty<ComponentSettings?> SettingsProperty = AvaloniaProperty.Register<ComponentPresenter, ComponentSettings?>(
        nameof(Settings));

    public static readonly StyledProperty<bool> IsPresentingSettingsProperty = AvaloniaProperty.Register<ComponentPresenter, bool>(
        nameof(IsPresentingSettings));

    public bool IsPresentingSettings
    {
        get => GetValue(IsPresentingSettingsProperty);
        set => SetValue(IsPresentingSettingsProperty, value);
    }

    public static readonly StyledProperty<bool> HideOnRuleProperty = AvaloniaProperty.Register<ComponentPresenter, bool>(
        nameof(HideOnRule));

    public bool HideOnRule
    {
        get => GetValue(HideOnRuleProperty);
        set => SetValue(HideOnRuleProperty, value);
    }
    
    public static readonly StyledProperty<Models.Ruleset.Ruleset?> HidingRulesProperty = AvaloniaProperty.Register<ComponentPresenter, Models.Ruleset.Ruleset?>(
        nameof(HidingRules));

    public Models.Ruleset.Ruleset? HidingRules
    {
        get => GetValue(HidingRulesProperty);
        set => SetValue(HidingRulesProperty, value);
    }
    
    public static readonly StyledProperty<bool> IsOnMainWindowProperty = AvaloniaProperty.Register<ComponentPresenter, bool>(
        nameof(IsOnMainWindow));

    public bool IsOnMainWindow
    {
        get => GetValue(IsOnMainWindowProperty);
        set => SetValue(IsOnMainWindowProperty, value);
    }

    public static readonly StyledProperty<bool> IsRootComponentProperty = AvaloniaProperty.Register<ComponentPresenter, bool>(
        nameof(IsRootComponent));

    public bool IsRootComponent
    {
        get => GetValue(IsRootComponentProperty);
        set => SetValue(IsRootComponentProperty, value);
    }

    private bool _isAllComponentsHid;

    public static readonly RoutedEvent<RoutedEventArgs> ComponentVisibilityChangedEvent =
        RoutedEvent.Register<ComponentPresenter, RoutedEventArgs>(nameof(ComponentVisibilityChanged),
            RoutingStrategies.Bubble);

    // Provide CLR accessors for adding and removing an event handler.
    public event EventHandler<RoutedEventArgs> ComponentVisibilityChanged
    {
        add { AddHandler(ComponentVisibilityChangedEvent, value); }
        remove { RemoveHandler(ComponentVisibilityChangedEvent, value); }
    }

    private static void PropertyChangedCallback(ComponentPresenter d, AvaloniaPropertyChangedEventArgs e)
    {
        d.UpdateContent(e.OldValue as ComponentSettings);
    }

    private IRulesetService RulesetService { get; } = IAppHost.GetService<IRulesetService>();

    private object? _presentingContent;

    public ComponentSettings? Settings
    {
        get => GetValue(SettingsProperty);
        set => SetValue(SettingsProperty, value);
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
            content.DataContext = TopLevel.GetTopLevel(this)?.DataContext;
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
                if (Resources.ContainsKey(key))
                {
                    Resources.Remove(key);
                }
            }
        }

        ControlColorHelper.SetControlForegroundColor(this, Settings.ForegroundColor, 
            Settings.IsCustomForegroundColorEnabled);
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
    
    static ComponentPresenter()
    {
        SettingsProperty.Changed.AddClassHandler<ComponentPresenter>(PropertyChangedCallback);
    }

    public ComponentPresenter()
    {
        InitializeComponent();

        this.GetObservable(HidingRulesProperty).Subscribe(new AnonymousObserver<Models.Ruleset.Ruleset?>(_ => UpdateWindowRuleState()));
        this.GetObservable(IsOnMainWindowProperty).Subscribe(new AnonymousObserver<bool>(_ => UpdateTheme()));
        this.GetObservable(HideOnRuleProperty).Subscribe(new AnonymousObserver<bool>(_ => UpdateWindowRuleState()));
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
            IsVisible = true;
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
            IsVisible = false;
        }
        else
        {
            IsVisible = true;
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
        if (Settings != null) Settings.IsVisible = IsVisible && !_isAllComponentsHid;
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
