using Avalonia;
using Avalonia.Controls;
using Avalonia.Reactive;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Models.Automation;
namespace ClassIsland.Core.Controls.Automation;

/// <summary>
/// TriggerSettingsControlPresenter.xaml 的交互逻辑
/// </summary>
public partial class TriggerSettingsControlPresenter : UserControl
{
    public static readonly StyledProperty<TriggerSettings?> SettingsProperty = AvaloniaProperty.Register<TriggerSettingsControlPresenter, TriggerSettings?>(
        nameof(Settings));

    public TriggerSettings? Settings
    {
        get => GetValue(SettingsProperty);
        set => SetValue(SettingsProperty, value);
    }

    public static readonly StyledProperty<string?> IdProperty = AvaloniaProperty.Register<TriggerSettingsControlPresenter, string?>(
        nameof(Id));

    public string? Id
    {
        get => GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }

    public TriggerSettingsControlPresenter()
    {
        InitializeComponent();

        this.GetObservable(IdProperty).Subscribe(new AnonymousObserver<string?>(_ => UpdateContent()));
        this.GetObservable(SettingsProperty).Subscribe(new AnonymousObserver<TriggerSettings?>(_ => UpdateContent()));
    }

    private void UpdateContent()
    {
        if (Settings is not { AssociatedTriggerInfo: not null })
        {
            return;
        }


        var settings = Settings.Settings;
        RootContentPresenter.Content = TriggerSettingsControlBase.GetInstance(Settings.AssociatedTriggerInfo, ref settings);
        Settings.Settings = settings;
    }
}
