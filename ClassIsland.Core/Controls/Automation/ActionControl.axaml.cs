using Avalonia;
using Avalonia.Controls;
using ClassIsland.Core.Models.Automation;
using ClassIsland.Shared.Models.Automation;
using CommunityToolkit.Mvvm.Input;
namespace ClassIsland.Core.Controls.Automation;

/// <summary>
/// 用于显示和编辑 <see cref="ActionSet"/>（行动组）的控件。
/// </summary>
/// <seealso cref="ActionItemControl"/>
public partial class ActionControl : UserControl
{
    /// <inheritdoc cref="ActionControl" />
    public ActionControl() => InitializeComponent();

    [RelayCommand]
    void AddAction(ActionMenuTreeItem menu)
    {
        var actionItem = new ActionItem(menu.ActionItemId);

        if (menu.GetType().IsGenericType &&
            menu.GetType().GetGenericTypeDefinition() == typeof(ActionMenuTreeItem<>))
        {
            var settingsType = menu.GetType().GetGenericArguments()[0];
            var settings = Activator.CreateInstance(settingsType);

            var setterProperty = menu.GetType()
                .GetProperty(nameof(ActionMenuTreeItem<object>.ActionItemSettingsSetter));
            var setter = setterProperty?.GetValue(menu) as Delegate;
            setter?.DynamicInvoke(settings);
            actionItem.Settings = settings;
        }

        actionItem.IsNewAdded = true;
        ActionSet.ActionItems.Add(actionItem);
    }

    /// <inheritdoc cref="ActionSet"/>
    public static readonly StyledProperty<ActionSet> ActionSetProperty =
        AvaloniaProperty.Register<ActionControl, ActionSet>(nameof(ActionSet));

    /// <summary>
    /// 要显示的行动组。
    /// </summary>
    public ActionSet ActionSet
    {
        get => GetValue(ActionSetProperty);
        set => SetValue(ActionSetProperty, value);
    }
}