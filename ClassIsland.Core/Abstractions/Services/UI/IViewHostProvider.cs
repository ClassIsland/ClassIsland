using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Enums.UI;

namespace ClassIsland.Core.Abstractions.Services.UI;

/// <summary>
/// 视图宿主提供方。
/// </summary>
public interface IViewHostProvider
{
    /// <summary>
    /// 获取一个视图宿主。
    /// </summary>
    /// <param name="activationPreference">视图激活偏好</param>
    /// <returns>获得的视图宿主</returns>
    IViewHost GetViewHost(ViewActivationPreference activationPreference);

    /// <summary>
    /// 当前试图宿主提供方实例
    /// </summary>
    public static IViewHostProvider Instance { get; internal set; } = new ViewHostProviderStub();

    /// <inheritdoc />
    class ViewHostProviderStub : IViewHostProvider
    {
        internal ViewHostProviderStub()
        {
        }
        
        /// <inheritdoc />
        public IViewHost GetViewHost(ViewActivationPreference activationPreference)
        {
            throw new NotImplementedException();
        }
    }
}