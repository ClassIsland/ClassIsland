using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ClassIsland.iOS.Services.Overlay;

/// <summary>
/// 调用 Swift 侧画中画桥的 C ABI。所有函数都必须在主线程（Avalonia UI 线程）调用。
/// </summary>
[SupportedOSPlatform("ios15.0")]
internal static partial class IosPipNativeBridge
{
    private const string NativeLibrary =
        "@rpath/ClassIslandLiveActivityBridge.framework/ClassIslandLiveActivityBridge";

    // 必须与 ClassIslandPipBridge.swift 中的 PipError 保持一致。
    internal const int Ok = 0;
    internal const int ErrorNotPrepared = -3;
    internal const int ErrorNotPossible = -4;

    [LibraryImport(NativeLibrary, EntryPoint = "ci_pip_is_supported")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int IsSupported();

    [LibraryImport(NativeLibrary, EntryPoint = "ci_pip_prepare")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int Prepare();

    [LibraryImport(NativeLibrary, EntryPoint = "ci_pip_set_auto_start")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void SetAutoStart(int enabled);

    [LibraryImport(NativeLibrary, EntryPoint = "ci_pip_enqueue_bgra")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int EnqueueBgra(nint pixels, int width, int height, int stride);

    [LibraryImport(NativeLibrary, EntryPoint = "ci_pip_start")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int Start();

    [LibraryImport(NativeLibrary, EntryPoint = "ci_pip_stop")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void Stop();

    [LibraryImport(NativeLibrary, EntryPoint = "ci_pip_teardown")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void Teardown();

    [LibraryImport(NativeLibrary, EntryPoint = "ci_pip_is_active")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int IsActive();

    [LibraryImport(NativeLibrary, EntryPoint = "ci_pip_is_possible")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int IsPossible();

    [LibraryImport(NativeLibrary, EntryPoint = "ci_pip_last_error")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint LastErrorPointer();

    internal static string GetLastError()
    {
        var pointer = LastErrorPointer();
        return pointer == 0 ? string.Empty : Marshal.PtrToStringUTF8(pointer) ?? string.Empty;
    }
}
