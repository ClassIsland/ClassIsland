using ClassIsland.Shared.Enums;

namespace ClassIsland.Shared.Models.Management;

/// <summary>
/// 集控清单。
/// </summary>
public class ManagementManifest
{
    /// <summary>
    /// 课表源
    /// </summary>
    public ReVersionString ClassPlanSource { get; set; } = new();

    /// <summary>
    /// 时间表源
    /// </summary>
    public ReVersionString TimeLayoutSource { get; set; } = new();

    /// <summary>
    /// 科目源
    /// </summary>
    public ReVersionString SubjectsSource { get; set; } = new();

    /// <summary>
    /// 默认设置源
    /// </summary>
    public ReVersionString DefaultSettingsSource { get; set; } = new();

    /// <summary>
    /// 策略源
    /// </summary>
    public ReVersionString PolicySource { get; set; } = new();
    
    /// <summary>
    /// 组件设置源
    /// </summary>
    public ReVersionString ComponentsSource { get; set; } = new();
    
    /// <summary>
    /// 凭据设置源
    /// </summary>
    public ReVersionString CredentialSource { get; set; } = new();

    /// <summary>
    /// 集控服务器类型
    /// </summary>
    public ManagementServerKind ServerKind { get; set; } = ManagementServerKind.Serverless;

    /// <summary>
    /// 组织名称
    /// </summary>
    public string OrganizationName { get; set; } = "组织名称";

    /// <summary>
    /// 集控信息的核心版本
    /// </summary>
    public Version CoreVersion { get; set; } = new Version(0, 0, 0, 0);
}