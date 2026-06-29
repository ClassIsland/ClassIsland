using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services.UI;
using ClassIsland.Core.Enums.UI;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Services.UI;

/// <summary>
/// 视图管理服务。
/// </summary>
public class ViewManagementService
{
    /// <summary>
    /// 服务实例。
    /// </summary>
    public static ViewManagementService Instance { get; } = new();

    private ViewManagementService()
    {
    }
    
    /// <summary>
    /// 激活视图到新的视图宿主上。
    /// </summary>
    /// <param name="viewBase">要激活的视图宿主</param>
    /// <param name="activationPreference">视图激活偏好</param>
    /// <returns></returns>
    public IViewHost ActivateView(ViewBase viewBase,
        ViewActivationPreference activationPreference = ViewActivationPreference.Default)
    {
        var viewHost = IViewHostProvider.Instance.GetViewHost(activationPreference);
        viewHost.ActivateView(viewBase);
        return viewHost;
    }

    /// <summary>
    /// 激活新的视图实例。
    /// </summary>
    /// <param name="postSetup">视图初始化后要执行的操作</param>
    /// <param name="activationPreference">视图激活偏好</param>
    /// <param name="serviceProvider">服务提供方</param>
    /// <typeparam name="T">要激活的视图类型</typeparam>
    /// <returns></returns>
    public T ActivateNewView<T>(Action<T>? postSetup = null,
        ViewActivationPreference activationPreference = ViewActivationPreference.Default,
        IServiceProvider? serviceProvider = null) where T : ViewBase
    {
        var instance = serviceProvider != null
            ? ActivatorUtilities.CreateInstance<T>(serviceProvider)
            : Activator.CreateInstance<T>();
        postSetup?.Invoke(instance);
        ActivateView(instance, activationPreference);
        return instance;
    }
}