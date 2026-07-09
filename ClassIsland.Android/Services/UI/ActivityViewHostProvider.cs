using System.Runtime.Versioning;
using ClassIsland.Android.Controls.UI;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services.UI;
using ClassIsland.Core.Enums.UI;

namespace ClassIsland.Android.Services.UI;

[SupportedOSPlatform("android24.0")]
public class ActivityViewHostProvider : IViewHostProvider
{
    public static ActivityViewHostProvider Instance { get; } = new();
    
    private static ViewActivationPreference DefaultPreference => ViewActivationPreference.ExistedViewHost;
    
    public HashSet<AndroidViewHost> ViewHosts { get; } = [];

    private ActivityViewHostProvider()
    {
        IViewHostProvider.Instance = this;
    }

    public IViewHost GetViewHost(ViewActivationPreference activationPreference)
    {
        activationPreference = activationPreference == ViewActivationPreference.Default
            ? DefaultPreference
            : activationPreference;

        return activationPreference switch
        {
            ViewActivationPreference.ExistedViewHost => ViewHosts.LastOrDefault() ?? CreateNew(),
            _ => CreateNew()
        };
    }

    private AndroidViewHost CreateNew()
    {
        throw new InvalidOperationException("当前没有可用的 Android Activity 视图宿主。");
    }
}
