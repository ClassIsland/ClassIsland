namespace ClassIsland.Core.Models.Management;

/// <summary>
/// 限制策略
/// </summary>
public class ManagementPolicy
{
    public bool DisableProfileClassPlanEditing { get; set; } = false;

    public bool DisableProfileTimeLayoutEditing { get; set; } = false;

    public bool DisableProfileSubjectsEditing { get; set; } = false;

    public bool DisableProfileEditing { get; set; } = false;

    public bool DisableSettingsEditing { get; set; } = false;

    public bool DisableSplashCustomize { get; set; } = false;

    public bool DisableDebugMenu { get; set; } = false;
}