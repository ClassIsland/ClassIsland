using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Models.SettingsWindow;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views.SettingPages;

[HidePageTitle]
[SettingsPageInfo(PageId, "设置目录", hideDefault: true, category: SettingsPageCategory.About)]
public partial class SettingsNavigationPage : SettingsPageBase
{
    internal const string PageId = "_navigation";
    internal const string PathParameterName = "path";

    internal string? GroupId { get; set; }
    internal event EventHandler<SettingsNavigationNode>? NodeInvoked;

    public SettingsNavigationPage()
    {
        InitializeComponent();
    }

    internal string? UpdateSections(IReadOnlyList<SettingsNavigationSection> sections)
    {
        if (GroupId == null)
        {
            SectionsList.ItemsSource = sections;
            return null;
        }

        var group = sections.SelectMany(section => section.Nodes).FirstOrDefault(node => node.GroupId == GroupId);
        SectionsList.ItemsSource = group?.Children is { } children
            ? new[] { new SettingsNavigationSection(children.Select(page => new SettingsNavigationNode(page)).ToArray(), false) }
            : Array.Empty<SettingsNavigationSection>();
        return group?.Name;
    }

    private void Node_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is FASettingsExpander { DataContext: SettingsNavigationNode node })
        {
            NodeInvoked?.Invoke(this, node);
        }
    }
}

internal sealed record SettingsNavigationNode(SettingsPageInfo Page, SettingsPageGroupInfo? Group = null,
    IReadOnlyList<SettingsPageInfo>? Children = null)
{
    public string Name => Group?.Name ?? Page.Name;
    public FAIconSource? IconSource => Group == null ? Page.UnSelectedIconSource : Group.IconSource;
    public string? GroupId => Group == null ? null : Page.GroupId;
}

internal sealed record SettingsNavigationSection(IReadOnlyList<SettingsNavigationNode> Nodes, bool ShowSeparator);
