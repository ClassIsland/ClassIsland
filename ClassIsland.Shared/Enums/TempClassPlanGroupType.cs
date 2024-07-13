namespace ClassIsland.Shared.Enums;

/// <summary>
/// 临时课表群启用类型
/// </summary>
public enum TempClassPlanGroupType
{
    /// <summary>
    /// 覆盖现在启用的课表群
    /// </summary>
    Override,
    /// <summary>
    /// 继承现在启用的课表群。如果当前临时课表群没有满足启用规则的课表，会在当前启用的课表群中查找课表。
    /// </summary>
    Inherit
}