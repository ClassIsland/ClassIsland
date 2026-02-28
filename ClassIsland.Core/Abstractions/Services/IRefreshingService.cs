namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 翻新与换届服务
/// </summary>
public interface IRefreshingService
{
    /// <summary>
    /// 初始化。
    /// </summary>
    internal Task Initialize();

    /// <summary>
    /// 显示迎新提示。
    /// </summary>
    /// <param name="isTest">是否是迎新测试模式</param>
    Task ShowOnboardingDialog(bool isTest=false);

    /// <summary>
    /// 开始翻新。
    /// </summary>
    /// <param name="isOnboarding">是否是迎新模式</param>
    Task BeginRefresh(bool isOnboarding=false);
}