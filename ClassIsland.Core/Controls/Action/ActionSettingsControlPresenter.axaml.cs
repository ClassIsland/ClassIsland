using Avalonia;
using Avalonia.Controls;
using Avalonia.Reactive;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Action;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Controls.Action;

/// <summary>
/// 行动设置控件显示控件。
/// </summary>
public partial class ActionSettingsControlPresenter : UserControl
{
    public static readonly StyledProperty<ActionItem?> ActionItemProperty =
        AvaloniaProperty.Register<ActionSettingsControlPresenter, ActionItem?>(nameof(ActionItem));

    public ActionItem? ActionItem
    {
        get => GetValue(ActionItemProperty);
        set => SetValue(ActionItemProperty, value);
    }

    public ActionSettingsControlPresenter()
    {
        InitializeComponent();
        this.GetObservable(ActionItemProperty).Subscribe(new AnonymousObserver<ActionItem?>(_ => UpdateContent()));
    }

    void UpdateContent()
    {
        if (string.IsNullOrEmpty(ActionItem?.Id))
        {
            RootContentPresenter.Content = null;
            return;
        }

        RootContentPresenter.Content = ActionSettingsControlBase.GetInstance(ActionItem);
    }
}
