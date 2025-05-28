using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Reactive;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using A = ClassIsland.Core.Models.Action;
namespace ClassIsland.Core.Controls.Action;

/// <summary>
/// 行动设置控件显示控件。
/// </summary>
public partial class ActionSettingsControlPresenter : UserControl
{
    public static readonly StyledProperty<string> ActionIdProperty = AvaloniaProperty.Register<ActionSettingsControlPresenter, string>(
        nameof(ActionId));

    public string ActionId
    {
        get => GetValue(ActionIdProperty);
        set => SetValue(ActionIdProperty, value);
    }

    public static readonly StyledProperty<Shared.Models.Action.Action?> ActionProperty = AvaloniaProperty.Register<ActionSettingsControlPresenter, Shared.Models.Action.Action?>(
        nameof(Action));

    public Shared.Models.Action.Action? Action
    {
        get => GetValue(ActionProperty);
        set => SetValue(ActionProperty, value);
    }

    public ActionSettingsControlPresenter()
    {
        InitializeComponent();
        this.GetObservable(ActionIdProperty).Subscribe(new AnonymousObserver<string>(_ => UpdateContent()));
        this.GetObservable(ActionProperty).Subscribe(new AnonymousObserver<Shared.Models.Action.Action?>(_ => UpdateContent()));
    }

    private void UpdateContent()
    {
        if (Action == null || ActionId == null)
        {
            return;
        }
        if (!IActionService.Actions.TryGetValue(ActionId, out var info))
        {
            return;
        }

        var actionSettings = Action.Settings;
        RootContentPresenter.Content = ActionSettingsControlBase.GetInstance(info, ref actionSettings);
        Action.Settings = actionSettings;
    }
}
