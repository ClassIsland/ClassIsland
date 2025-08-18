using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Automation;
using ClassIsland.Core.Models.UI;
using ClassIsland.Shared.Models.Automation;
using CommunityToolkit.Mvvm.Input;
namespace ClassIsland.Core.Controls.Automation;

/// <summary>
/// 用于显示和编辑 <see cref="ActionItem"/>（行动项）的控件。
/// </summary>
/// <seealso cref="ActionControl"/>
public partial class ActionItemControl : UserControl
{
    public ActionItemControl() => InitializeComponent();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateContent();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Unload();
    }

    void UpdateContent()
    {
        ActionInfoIconText.Glyph =
            IActionService.ActionInfos.TryGetValue(ActionItem.Id, out var actionInfo) ? actionInfo.IconGlyph : "\uee31";
        ActionInfoIconText.Text = actionInfo?.Name ?? $"{ActionItem.Id}（未知行动）";

        var newControl = ActionSettingsControlBase.GetInstance(ActionItem);
        if (newControl != null)
        {
            newControl.ActionNameChanged += ControlOnActionNameChanged;
            newControl.ActionIconChanged += ControlOnActionIconChanged;
            RootContentPresenter.Content = newControl;
            RootContentPresenter.IsVisible = true;
            if (ActionItem.IsNewAdded)
                newControl.Loaded += ControlOnLoaded;
        }
        else
        {
            RootContentPresenter.Content = null;
            RootContentPresenter.IsVisible = false;
        }
        ActionItem.IsNewAdded = false;
    }

    void ControlOnLoaded(object? sender, RoutedEventArgs routedEventArgs) =>
        (sender as ActionSettingsControlBase).NotifyAdded();

    void ControlOnActionNameChanged(object? sender, string e) => ActionInfoIconText.Text = e;
    void ControlOnActionIconChanged(object? sender, string? e) => ActionInfoIconText.Glyph = e;


    void Unload()
    {
        if (RootContentPresenter.Content is ActionSettingsControlBase oldControl)
        {
            oldControl.ActionNameChanged -= ControlOnActionNameChanged;
            oldControl.ActionIconChanged -= ControlOnActionIconChanged;
            oldControl.Loaded -= ControlOnLoaded;
        }

        RootContentPresenter.Content = null;
    }



    [RelayCommand]
    void ChangeAction((object T1, object T2) p)
    {
        if (p is { T1: ActionItem actionItem, T2: ActionMenuTreeItem menu })
        {
            actionItem.Id = menu.ActionItemId;

            if (menu.GetType().IsGenericType &&
                menu.GetType().GetGenericTypeDefinition() == typeof(ActionMenuTreeItem<>))
            {
                var settingsType = menu.GetType().GetGenericArguments()[0];
                var settings = actionItem.Settings;
                if (settings?.GetType() != settingsType)
                {
                    Unload();
                    ActionItem.IsNewAdded = true;
                    settings = Activator.CreateInstance(settingsType);
                }

                var setterProperty = menu.GetType()
                    .GetProperty(nameof(ActionMenuTreeItem<object>.ActionItemSettingsSetter));
                var setter = setterProperty?.GetValue(menu) as Delegate;
                setter?.DynamicInvoke(settings);
                actionItem.Settings = settings;
            }
            else
            {
                Unload();
                ActionItem.IsNewAdded = true;
            }

            UpdateContent();
        }
    }

    [RelayCommand]
    void RemoveAction(ActionItem actionItem)
    {
        if (actionItem.IsWorking) return;

        if (RootContentPresenter.Content is ActionSettingsControlBase controlBase)
        {
            if (controlBase.ShouldShowUndoDeleteButton())
            {
                var index = ActionSet.ActionItems.IndexOf(actionItem);

                var revertButton = new Button { Content = "撤销" };
                var toastMessage = new ToastMessage($"已删除行动“{ActionInfoIconText.Text}”。")
                {
                    ActionContent = revertButton,
                    Duration = TimeSpan.FromSeconds(10)
                };
                revertButton.Click += (o, args) =>
                {
                    ActionSet.ActionItems.Insert(Math.Min(index, ActionSet.ActionItems.Count), actionItem);
                    toastMessage.Close();
                };
                this.ShowToast(toastMessage);
            }
        }

        ActionSet.ActionItems.Remove(actionItem);
    }




    public static readonly StyledProperty<ActionItem> ActionItemProperty =
        AvaloniaProperty.Register<ActionItemControl, ActionItem>(nameof(ActionItem));

    public ActionItem ActionItem
    {
        get => GetValue(ActionItemProperty);
        set => SetValue(ActionItemProperty, value);
    }

    public ActionSet ActionSet => this.FindAncestorOfType<ActionControl>()?.GetValue(ActionControl.ActionSetProperty);
}
