using ClassIsland.Core.Attributes;
namespace ClassIsland.Core.Helpers;

/// <summary>
/// 注册上下文跟踪。
/// </summary>
static class RegistryContext
{
    /// <summary>
    /// 最近注册项的 <see cref="ContributorInfo"/> 引用。
    /// </summary>
    public static ContributorInfo LastContributorInfo { get; set; } = null!;
}
