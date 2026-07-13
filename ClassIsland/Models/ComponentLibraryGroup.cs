using System.Collections.ObjectModel;
using ClassIsland.Core.Attributes;

namespace ClassIsland.Models;

public class ComponentLibraryGroup(ContributorInfo? contributorInfo)
{
    public ContributorInfo? ContributorInfo { get; } = contributorInfo;

    public string GroupName => ContributorInfo?.PluginName ?? "ClassIsland";

    public bool HasContributor => ContributorInfo != null;

    public ObservableCollection<ComponentInfo> Items { get; } = new();
}
