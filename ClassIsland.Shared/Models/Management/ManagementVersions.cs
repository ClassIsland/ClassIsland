namespace ClassIsland.Shared.Models.Management;

/// <summary>
/// 集控本地信息版本
/// </summary>
public class ManagementVersions
{
    /// <summary>
    /// 课表版本
    /// </summary>
    public int ClassPlanVersion { get; set; } = 0;

    /// <summary>
    /// 时间表版本
    /// </summary>
    public int TimeLayoutVersion { get; set; } = 0;

    /// <summary>
    /// 科目版本
    /// </summary>
    public int SubjectsVersion { get; set; } = 0;

    /// <summary>
    /// 默认设置版本
    /// </summary>
    public int DefaultSettingsVersion { get; set; } = 0;

    /// <summary>
    /// 策略版本
    /// </summary>
    public int PolicyVersion { get; set; } = 0;
}