using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Shared.Helpers;
using CommunityToolkit.Mvvm.Input;

namespace ClassIsland.Core.Controls.Ruleset;

/// <summary>
/// RulesetControl.xaml 的交互逻辑
/// </summary>
public partial class RulesetControl : UserControl
{
    public static readonly StyledProperty<Models.Ruleset.Ruleset> RulesetProperty = AvaloniaProperty.Register<RulesetControl, Models.Ruleset.Ruleset>(
        nameof(Ruleset));

    public Models.Ruleset.Ruleset Ruleset
    {
        get => GetValue(RulesetProperty);
        set => SetValue(RulesetProperty, value);
    }

    public static readonly StyledProperty<bool> ShowTitleProperty = AvaloniaProperty.Register<RulesetControl, bool>(
        nameof(ShowTitle));

    public bool ShowTitle
    {
        get => GetValue(ShowTitleProperty);
        set => SetValue(ShowTitleProperty, value);
    }

    /// <inheritdoc />
    public RulesetControl()
    {
        InitializeComponent();
    }

    private void ButtonAddRule_OnClick(object sender, RoutedEventArgs e)
    {
        Ruleset.Groups.Add(new RuleGroup());
    }

    [RelayCommand]
    private void AddRule(RuleGroup group)
    {
        group.Rules.Add(new Rule());
    }

    [RelayCommand]
    private void RemoveRule(Rule rule)
    {
        foreach (var group in Ruleset.Groups)
        {
            group.Rules.Remove(rule);
        }
    }

    [RelayCommand]
    private void RemoveGroup(RuleGroup group)
    {
        Ruleset.Groups.Remove(group);
    }

    [RelayCommand]
    private void DuplicateGroup(RuleGroup group)
    {
        Ruleset.Groups.Add(ConfigureFileHelper.CopyObject(group));
    }

    private void ScrollViewerRulesetActions_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (sender is not ScrollViewer viewer) return;

        var before = viewer.Offset.X;
        if (e.Delta.Y > 0)
        {
            viewer.PageLeft();
        }
        else
        {
            viewer.PageRight();
        }

        if (viewer.Offset.X != before)
        {
            e.Handled = true;
        }
    }
}