using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using A = ClassIsland.Core.Models.Action;
namespace ClassIsland.Core.Controls.Action;

/// <summary>
/// 行动设置控件显示控件。
/// </summary>
public partial class ActionSettingsControlPresenter : UserControl
{
    public static readonly DependencyProperty ActionIdProperty = DependencyProperty.Register(
        nameof(ActionId), typeof(string), typeof(ActionSettingsControlPresenter), new PropertyMetadata(default(string), (o, _) =>
        {
            if (o is ActionSettingsControlPresenter control)
            {
                control.UpdateContent();
            }
        }));

    public string ActionId
    {
        get => (string)GetValue(ActionIdProperty);
        set => SetValue(ActionIdProperty, value);
    }

    public static readonly DependencyProperty ActionProperty = DependencyProperty.Register(
        nameof(Action), typeof(Shared.Models.Action.Action), typeof(ActionSettingsControlPresenter), new PropertyMetadata(default(Shared.Models.Action.Action), (o, _) =>
        {
            if (o is ActionSettingsControlPresenter control)
            {
                control.UpdateContent();
            }
        }));

    public Shared.Models.Action.Action? Action
    {
        get => (Shared.Models.Action.Action)GetValue(ActionProperty);
        set => SetValue(ActionProperty, value);
    }

    public ActionSettingsControlPresenter()
    {
        InitializeComponent();
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