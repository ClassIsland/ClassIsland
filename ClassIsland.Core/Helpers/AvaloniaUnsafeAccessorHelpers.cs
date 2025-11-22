using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Vulkan;

namespace ClassIsland.Core.Helpers;

internal static class AvaloniaUnsafeAccessorHelpers
{
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_Current")]
    static extern IAvaloniaDependencyResolver? GetCurrentAvaloniaLocator(AvaloniaLocator? nullLocator);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetService")]
    static extern object? GetAvaloniaDependencyService(IAvaloniaDependencyResolver? avaloniaLocator, Type serviceType);

    private static IAvaloniaDependencyResolver? AvaloniaLocator { get; }= GetCurrentAvaloniaLocator(null);

    internal static T? GetAvaloniaLocatorService<T>()
        where T : class
    {
        if (AvaloniaLocator is null)
            return null;
        object? result = GetAvaloniaDependencyService(AvaloniaLocator, typeof(T));
        return result as T;
    }
    
    public static Win32CompositionMode? GetActiveWin32CompositionMode()
    {
        var renderTimer = GetAvaloniaLocatorService<IRenderTimer>();
        var renderTimerClassName = renderTimer?.GetType().Name;
        var win32CompositionMode = renderTimerClassName switch
        {
            "WinUiCompositorConnection" => Win32CompositionMode.WinUIComposition,
            "DirectCompositionConnection" => Win32CompositionMode.DirectComposition,
            "DxgiConnection" => Win32CompositionMode.LowLatencyDxgiSwapChain,
            _ => Win32CompositionMode.RedirectionSurface
        };
        return win32CompositionMode;
    }

    public static Win32ActiveRenderingMode? GetActiveWin32RenderingMode()
    {
        var platformGraphics = GetAvaloniaLocatorService<IPlatformGraphics>();
        var platformGraphicsClassName = platformGraphics?.GetType().Name;
        return platformGraphicsClassName switch
        {
            null when GetAvaloniaLocatorService<Win32PlatformOptions>()?.CustomPlatformGraphics is not null => Win32ActiveRenderingMode.Custom,
            null => Win32ActiveRenderingMode.Software,
            "D3D9AngleWin32PlatformGraphics" => Win32ActiveRenderingMode.AngleEglD3D9,
            "D3D11AngleWin32PlatformGraphics" => Win32ActiveRenderingMode.AngleEglD3D11,
            "WglPlatformOpenGlInterface" => Win32ActiveRenderingMode.Wgl,
            nameof(VulkanPlatformGraphics) => Win32ActiveRenderingMode.Vulkan,
            _ => null
        };
    }

    public enum Win32ActiveRenderingMode
    {
        Custom,
        Software,
        AngleEglD3D9,
        AngleEglD3D11,
        Wgl,
        Vulkan
    }
}