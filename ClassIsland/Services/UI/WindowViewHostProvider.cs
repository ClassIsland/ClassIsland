using System;
using System.Collections.Generic;
using System.Linq;
using ClassIsland.Controls.UI;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services.UI;
using ClassIsland.Core.Enums.UI;

namespace ClassIsland.Services.UI;

public class WindowViewHostProvider(bool mobile) : IViewHostProvider
{
    private ViewActivationPreference DefaultPreference => mobile ? ViewActivationPreference.ExistedViewHost 
        : ViewActivationPreference.NewViewHost;
    
    private HashSet<WindowViewHost> ViewHosts { get; } = [];

    public IViewHost GetViewHost(ViewActivationPreference activationPreference)
    {
        activationPreference = activationPreference == ViewActivationPreference.Default
            ? DefaultPreference
            : activationPreference;
        var host = activationPreference switch
        {
            ViewActivationPreference.NewViewHost => CreateNew(),
            ViewActivationPreference.ExistedViewHost => ViewHosts.FirstOrDefault(x => x.IsActive)
                                                        ?? ViewHosts.LastOrDefault()
                                                        ?? CreateNew(),
            _ => CreateNew()
        };
        return host;
    }

    private WindowViewHost CreateNew()
    {
        var host = new WindowViewHost(mobile);
        host.Closed += HostOnClosed;
        ViewHosts.Add(host);
        return host;
        
        void HostOnClosed(object? sender, EventArgs e)
        {
            ViewHosts.Remove(host);
        }
    }
}