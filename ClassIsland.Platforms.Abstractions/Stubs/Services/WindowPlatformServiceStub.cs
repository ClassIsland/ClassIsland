using Avalonia.Controls;
using ClassIsland.Platforms.Abstraction.Enums;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <summary>
/// 平台窗口服务桩
/// </summary>
public class WindowPlatformServiceStub : IWindowPlatformService
{
    internal WindowPlatformServiceStub()
    {
        
    }
    
    /// <inheritdoc />
    public void SetWindowFeature(TopLevel toplevel, WindowFeatures features, bool state)
    {
    }

    /// <inheritdoc />
    public WindowFeatures GetWindowFeatures(TopLevel topLevel)
    {
        return (WindowFeatures)0;
    }

    /// <inheritdoc />
    public void RegisterForegroundWindowChangedEvent(EventHandler<ForegroundWindowChangedEventArgs> handler)
    {
    }
}