using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Vulkan;

namespace ClassIsland.Core.Helpers;

internal static class AvaloniaUnsafeAccessorHelpers
{
    // Gesture capture bypasses routed input. Avalonia hides the raw input observer from reference assemblies.
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_PreProcess")]
    internal static extern IObservable<RawInputEventArgs> GetRawInputPreProcess(IInputManager inputManager);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Device")]
    internal static extern IInputDevice GetRawInputDevice(RawInputEventArgs args);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "TryGetPointer")]
    internal static extern IPointer? GetRawInputPointer(IPointerDevice device, RawPointerEventArgs args);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Position")]
    internal static extern Point GetRawPointerPosition(RawPointerEventArgs args);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Type")]
    internal static extern RawPointerEventType GetRawPointerEventType(RawPointerEventArgs args);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ApplyTemplatedParent")]
    internal static extern void ApplyTemplatedParent(TemplatedControl? declaringType, StyledElement control,
        AvaloniaObject? templatedParent);

    // ComboBox exposes these events through its native popup; drawer selection has no native popup.
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "DropDownOpened")]
    internal static extern ref EventHandler? GetComboBoxDropDownOpened(ComboBox comboBox);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "DropDownClosed")]
    internal static extern ref EventHandler? GetComboBoxDropDownClosed(ComboBox comboBox);

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
        // In Avalonia 12, IRenderTimer is no longer registered in the AvaloniaLocator.
        // Composition connections register as IRenderLoop (via RenderLoop.FromTimer(connection)).
        // We retrieve the IRenderLoop, then extract the underlying IRenderTimer from
        // DefaultRenderLoop's private _timer field to determine the actual composition mode.
        var renderLoop = GetAvaloniaLocatorService<IRenderLoop>();
        if (renderLoop is null)
            return Win32CompositionMode.RedirectionSurface;

        var timerField = renderLoop.GetType().GetField("_timer",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var timer = timerField?.GetValue(renderLoop);
        var timerClassName = timer?.GetType().Name;

        return timerClassName switch
        {
            "WinUiCompositorConnection" => Win32CompositionMode.WinUIComposition,
            "DirectCompositionConnection" => Win32CompositionMode.DirectComposition,
            "DxgiConnection" => Win32CompositionMode.LowLatencyDxgiSwapChain,
            _ => Win32CompositionMode.RedirectionSurface
        };
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
