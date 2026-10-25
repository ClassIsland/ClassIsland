using ClassIsland.Controls.UI;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services.UI;
using ClassIsland.Core.Enums.UI;

namespace ClassIsland.iOS.Services.UI;

/// <summary>
/// iOS/iPadOS 单 Scene 使用的共享移动视图宿主提供方。
/// </summary>
internal sealed class IosViewHostProvider(MobileViewHost viewHost) : IViewHostProvider
{
    public IViewHost GetViewHost(ViewActivationPreference activationPreference) => viewHost;
}
