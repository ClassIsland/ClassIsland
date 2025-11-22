using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Automation;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
namespace ClassIsland.Core.Abstractions.Controls;

/// <summary>
/// 行动设置控件基类。
/// </summary>
/// <typeparam name="TSettings">行动设置类型。需要获取行动设置的行动设置控件须标注此类型。</typeparam>
/// <example>
/// FooActionSettingsControl.axaml
/// <code>
/// &lt;ci:ActionSettingsControlBase
///     x:Class="FooActionSettingsControl">
/// </code>
/// FooActionSettingsControl.axaml.cs
/// <code>
/// public class FooActionSettingsControl : ActionSettingsControlBase { }
/// </code>
///
/// BarActionSettingsControl.axaml
/// <code>
/// &lt;ci:ActionSettingsControlBase
///     x:Class="FooActionSettingsControl"
///     x:TypeArguments="BarActionSettings">
/// </code>
/// BarActionSettingsControl.axaml.cs
/// <code>
/// public class BarActionSettingsControl : ActionSettingsControlBase&lt;BarActionSettings> { }
/// </code>
/// </example>
public abstract class ActionSettingsControlBase : UserControl
{
    /// <summary>
    /// 当行动项被用户添加时，此方法将被调用。
    /// </summary>
    /// <remarks>
    /// 如需控制触发顺序，请重写 <see cref="OnAttachedToVisualTree"/>，此方法会在 base.<see cref="OnAttachedToVisualTree"/> 调用时触发。
    /// </remarks>
    /// <example>
    /// <code>
    /// public class FooActionSettingsControl : ActionSettingsControlBase
    /// {
    ///     protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    ///     {
    ///         base.OnAttachedToVisualTree(e); // OnAdded() 将在此处触发。
    ///         Bar();
    ///     }
    /// }
    /// </code>
    /// </example>
    protected virtual void OnAdded()
    {
        // if (!this.IsAttachedToVisualTree()) return;
        ActionItemAdded?.Invoke(this, EventArgs.Empty);
    }

    /// 当行动项被用户添加时，此事件将被调用。
    public event EventHandler? ActionItemAdded;

    /// <summary>
    /// 当行动项被用户删除时，此方法将被调用，以询问是否需要提供撤销删除按钮。
    /// </summary>
    /// <returns>
    /// 如果需要提供撤销删除按钮，请返回 true。默认返回 false。
    /// </returns>
    /// <seealso cref="ActionBase{TSettings}.Settings"/>
    protected virtual bool IsUndoDeleteRequested() => false;



    /// 调用此方法，以更改行动项显示的行动名。
    protected void ChangeActionName(string newName) =>
        ActionNameChanged?.Invoke(this, newName);

    /// 调用此方法，以更改行动项显示的图标时。
    protected void ChangeActionIcon(string? newIconGlyph) =>
        ActionIconChanged?.Invoke(this, newIconGlyph);

    /// 调用此方法，以打开抽屉并显示控件。
    /// <param name="control">要显示的 ContentControl 或 Control 控件。注意：此控件需设定宽度。</param>
    /// <param name="isOpenInDialog">优先在 Dialog 中打开。默认为 false，即优先在应用设置抽屉中打开。</param>
    protected async Task ShowDrawer(Control control, bool isOpenInDialog = false)
    {
        if (!isOpenInDialog &&
            this.GetVisualRoot() is Window window &&
            window.GetType().FullName == "ClassIsland.Views.SettingsWindowNew")
        {
            control.Classes.Remove("in-dialog");
            control.Classes.Add("in-drawer");
            if (control is ContentControl cc)
                cc.Padding = new(16);
            else
                control.Margin = new(16);
            SettingsPageBase.OpenDrawerCommand.Execute(control);
        }
        else
        {
            control.Classes.Remove("in-drawer");
            control.Classes.Add("in-dialog");

            if (control.Parent is ContentDialog contentDialog)
            {
                contentDialog.Content = null;
            }

            var dialog = new ContentDialog
            {
                Content = control,
                TitleTemplate = new DataTemplate(),
                PrimaryButtonText = "确定",
                DefaultButton = ContentDialogButton.Primary,
                DataContext = this
            };

            await dialog.ShowAsync(TopLevel.GetTopLevel(this));
        }
    }



    internal bool ShouldShowUndoDeleteButton() => IsUndoDeleteRequested();
    internal object? SettingsInternal { get; set; }
    internal event EventHandler<string>? ActionNameChanged;
    internal event EventHandler<string?>? ActionIconChanged;
    internal bool IsNewAdded = false;

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!IsNewAdded) return;
        IsNewAdded = false;
        OnAdded();
    }


    static Lazy<IActionService?> ActionService { get; } = new(IAppHost.TryGetService<IActionService>);

    /// <summary>
    /// 获取行动设置控件实例。
    /// </summary>
    /// <param name="actionItem">要获取行动设置控件的行动项。</param>
    public static ActionSettingsControlBase? GetInstance(ActionItem? actionItem)
    {
        if (string.IsNullOrEmpty(actionItem?.Id)) return null;

        // Bug：过于简单的控件会在此开始加载 AXAML，此时 Settings 仍为 null。
        var control = IAppHost.Host?.Services.GetKeyedService<ActionSettingsControlBase>(actionItem.Id);
        if (control == null)
        {
            ActionService.Value?.MigrateUnknownActionItem(actionItem);
            control = IAppHost.Host?.Services.GetKeyedService<ActionSettingsControlBase>(actionItem.Id);
            if (control == null) return null;
        }

        var settingsType = control.GetType().BaseType?.GetGenericArguments().FirstOrDefault();
        if (settingsType != null)
        {
            if (actionItem.Settings is JsonElement json)
                actionItem.Settings = json.Deserialize(settingsType);
            if (actionItem.Settings?.GetType() != settingsType)
                actionItem.Settings = Activator.CreateInstance(settingsType);
        }

        control.SettingsInternal = actionItem.Settings;
        return control;
    }
}

/// <inheritdoc />
public abstract class ActionSettingsControlBase<TSettings> : ActionSettingsControlBase where TSettings : class
{
    /// <summary>
    /// 当前行动项的设置。注意：请勿在构造函数中访问。
    /// </summary>
    protected TSettings Settings =>
        SettingsInternal as TSettings ??
        throw new ArgumentNullException(nameof(Settings), $"过早访问行动项设置（{typeof(TSettings).FullName}）。");
}
