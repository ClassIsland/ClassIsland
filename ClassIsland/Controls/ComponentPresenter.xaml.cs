using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Components;
using ClassIsland.Core.Models.Ruleset;
using YamlDotNet.Core.Tokens;

namespace ClassIsland.Controls;

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
        nameof(HidingRules), typeof(Ruleset), typeof(ComponentPresenter), new PropertyMetadata(default(Ruleset),
            (o, args) =>
            {
                if (o is ComponentPresenter control)
                {
                    control.CheckHideRule();
                }
            }));

    public Ruleset? HidingRules
    {
        get { return (Ruleset)GetValue(HidingRulesProperty); }
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

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ComponentPresenter p)
        {
            p.UpdateContent(e.OldValue as ComponentSettings);
        }
    }

    private IRulesetService RulesetService { get; } = App.GetService<IRulesetService>();

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
        }
        if (Settings == null)
            return;
        Settings.PropertyChanged += SettingsOnPropertyChanged;
        var content = App.GetService<IComponentsService>().GetComponent(Settings, IsPresentingSettings);
        // 理论上展示的内容的数据上下文应为MainWindow，这里不便用前端xaml绑定，故在后台设置。
        if (content != null && IsOnMainWindow)
        {
            content.DataContext = Window.GetWindow(this);
        }

        PresentingContent = content;
        UpdateTheme();
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateTheme();
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
    }

    private void RulesetServiceOnStatusUpdated(object? sender, EventArgs e)
    {
        CheckHideRule();
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
}