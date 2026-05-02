using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.Actions;

namespace ClassIsland.Controls.ActionSettingsControls;

/// <summary>
/// NotificationActionSettingsControl.axaml 的交互逻辑
/// </summary>
public partial class NotificationActionSettingsControl : ActionSettingsControlBase<NotificationActionSettings>
{
    public NotificationActionSettingsControl() => InitializeComponent();

    protected override void OnAdded() => ShowNotificationActionSettingsDrawer();

    protected override bool IsUndoDeleteRequested() =>
        Settings.Mask.Length + Settings.Content.Length > 10;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Settings.PropertyChanged += SettingsOnPropertyChanged;
        UpdateActionName();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Settings.PropertyChanged -= SettingsOnPropertyChanged;
    }


    public void ButtonShowSettings_OnClick(object? sender, RoutedEventArgs e) =>
        ShowNotificationActionSettingsDrawer();

    void ShowNotificationActionSettingsDrawer()
    {
        if (this.FindResource("NotificationActionSettingsDrawer") is not ContentControl cc) return;
        cc.DataContext = this;
        _ = ShowDrawer(cc);
    }

    void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.IsWaitForCompleteEnabled))
            UpdateActionName();
    }

    void UpdateActionName()
    {
        ChangeActionName(!Settings.IsWaitForCompleteEnabled ? "显示提醒" : "显示提醒，并等待提醒结束");
    }
}
