using ClassIsland.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Management;

public class ManagementManifest
{
    public ReVersionString ClassPlanSource { get; set; } = new();

    public ReVersionString TimeLayoutSource { get; set; } = new();

    public ReVersionString SubjectsSource { get; set; } = new();

    public ReVersionString DefaultSettingsSource { get; set; } = new();

    public ReVersionString PolicySource { get; set; } = new();

    public ManagementServerKind ServerKind { get; set; } = ManagementServerKind.Serverless;

    public string OrganizationName { get; set; } = "组织名称";
}