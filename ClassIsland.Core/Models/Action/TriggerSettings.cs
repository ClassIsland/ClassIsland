using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;

namespace ClassIsland.Core.Models.Action;

/// <summary>
/// 代表一个触发器的设置。
/// </summary>
public class TriggerSettings
{
    /// <summary>
    /// 触发器 ID
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// 触发器设置
    /// </summary>
    public object? Settings { get; set; }

    /// <summary>
    /// 关联的触发器信息
    /// </summary>
    public TriggerInfo? AssociatedTriggerInfo => IAutomationService.RegisteredTriggers.FirstOrDefault(x => x.Id == Id);
}