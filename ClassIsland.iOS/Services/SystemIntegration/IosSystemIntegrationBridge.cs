using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.iOS.Services.SystemIntegration;

internal static partial class IosSystemIntegrationBridge
{
    private const string NativeLibrary =
        "@rpath/ClassIslandLiveActivityBridge.framework/ClassIslandLiveActivityBridge";

    public static event Action<Uri>? NavigationRequested;

    public static unsafe void RegisterNavigation() => RegisterNavigationNative(&OnNavigationRequested);

    public static unsafe void UnregisterNavigation() => RegisterNavigationNative(null);

    public static void ConsumePendingNavigation()
    {
        var value = Marshal.PtrToStringUTF8(ConsumePendingNative());
        if (AppNavigationUriParser.TryParseClassIslandUri(value, out var uri))
        {
            NavigationRequested?.Invoke(uri!);
        }
    }

    public static void Publish(string json)
    {
        if (PublishNative(json) == 0)
        {
            throw new InvalidOperationException(
                Marshal.PtrToStringUTF8(GetLastErrorNative()) ?? "Unable to publish the system schedule.");
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnNavigationRequested(nint pointer)
    {
        try
        {
            if (AppNavigationUriParser.TryParseClassIslandUri(Marshal.PtrToStringUTF8(pointer), out var uri))
            {
                NavigationRequested?.Invoke(uri!);
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"处理系统操作入口时发生异常：{exception}");
        }
    }

    [LibraryImport(NativeLibrary, EntryPoint = "ci_system_integration_register_navigation")]
    private static unsafe partial void RegisterNavigationNative(delegate* unmanaged[Cdecl]<nint, void> callback);

    [LibraryImport(NativeLibrary, EntryPoint = "ci_system_integration_consume_pending")]
    private static partial nint ConsumePendingNative();

    [LibraryImport(NativeLibrary, EntryPoint = "ci_system_integration_publish", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int PublishNative(string json);

    [LibraryImport(NativeLibrary, EntryPoint = "ci_system_integration_last_error")]
    private static partial nint GetLastErrorNative();
}
