using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ClassIsland.iOS.Services.Overlay;

/// <summary>
/// 调用 Swift 侧 HUD 桥的稳定 C ABI：拉起/终止 root 宿主进程，并把 Avalonia 窗口注册到 SpringBoard。
/// </summary>
[SupportedOSPlatform("ios15.0")]
internal static partial class IosHudNativeBridge
{
    private const string NativeLibrary =
        "@rpath/ClassIslandLiveActivityBridge.framework/ClassIslandLiveActivityBridge";

    [LibraryImport(NativeLibrary, EntryPoint = "ci_hud_start", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int Start(int spawnAsRoot, string dataDirectory);

    [LibraryImport(NativeLibrary, EntryPoint = "ci_hud_stop")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int Stop();

    [LibraryImport(NativeLibrary, EntryPoint = "ci_hud_is_running")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int IsRunning();

    [LibraryImport(NativeLibrary, EntryPoint = "ci_hud_get_pid")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int GetPid();

    [LibraryImport(NativeLibrary, EntryPoint = "ci_hud_last_error")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial nint LastErrorPointer();

    [LibraryImport(NativeLibrary, EntryPoint = "ci_hud_register_window")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void RegisterWindow(nint window);

    internal static string GetLastError()
    {
        var pointer = LastErrorPointer();
        return pointer == 0 ? string.Empty : Marshal.PtrToStringUTF8(pointer) ?? string.Empty;
    }
}
