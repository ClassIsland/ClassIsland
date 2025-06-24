using Avalonia.Controls;
using ClassIsland.Platforms.Abstraction.Enums;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platform.Windows.Services;

public class WindowPlatformService : IWindowPlatformService
{
    public void SetWindowFeature(TopLevel toplevel, WindowFeatures features, bool state)
    {
    }

    public WindowFeatures GetWindowFeatures(TopLevel topLevel)
    {
        return (WindowFeatures)0;
    }

    public void RegisterForegroundWindowChangedEvent(EventHandler<ForegroundWindowChangedEventArgs> handler)
    {
    }
}