using System.Collections.ObjectModel;
using ClassIsland.Core.Attributes;
namespace ClassIsland.Models;

public class ComponentLibraryGroup(ContributorInfo contributorInfo)
{
    ContributorInfo ContributorInfo { get; } = contributorInfo;

    public string GroupName => GetGroupName(ContributorInfo);

    public static string GetGroupName(ContributorInfo info) => info.IsBuiltIn ? "ClassIsland" : info.PluginName ?? "???";

    public ObservableCollection<ComponentInfo> Items { get; } = [];
}
