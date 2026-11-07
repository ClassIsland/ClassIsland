using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Foundation;
using SoundFlow.Backends.MiniAudio;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 在首次 SoundFlow P/Invoke 前初始化其内置 resolver，并预加载 iOS framework。
/// </summary>
internal static class IosSoundFlowNativeBootstrap
{
    private const string NativeTypeName = "SoundFlow.Backends.MiniAudio.Native";
    private static readonly object InitializationLock = new();
    private static nint _libraryHandle;

    [DynamicDependency(
        DynamicallyAccessedMemberTypes.All,
        NativeTypeName,
        "SoundFlow")]
    public static bool TryInitialize(
        [NotNullWhen(false)] out Exception? exception)
    {
        lock (InitializationLock)
        {
            if (_libraryHandle != 0)
            {
                exception = null;
                return true;
            }

            try
            {
                // Mono AOT 可能在首个 source-generated P/Invoke 前跳过此类型的
                // 静态构造器。显式运行它，让 SoundFlow 注册且仅注册自己的 resolver。
                var nativeType = typeof(MiniAudioEngine).Assembly.GetType(
                    NativeTypeName,
                    throwOnError: true)!;
                RuntimeHelpers.RunClassConstructor(nativeType.TypeHandle);

                var frameworksPath = NSBundle.MainBundle.PrivateFrameworksPath;
                if (string.IsNullOrWhiteSpace(frameworksPath))
                {
                    frameworksPath = Path.Combine(
                        NSBundle.MainBundle.BundlePath,
                        "Frameworks");
                }

                var binaryPath = Path.Combine(
                    frameworksPath,
                    "miniaudio.framework",
                    "miniaudio");
                if (!File.Exists(binaryPath))
                {
                    throw new DllNotFoundException(
                        $"SoundFlow iOS framework binary does not exist: {binaryPath}");
                }

                // 使用规范的 Frameworks 绝对路径，避免依赖安装/重签工具是否保留
                // SoundFlow runtimes 目录中的 resolver symlink。
                _libraryHandle = NativeLibrary.Load(binaryPath);
                exception = null;
                return true;
            }
            catch (Exception initializationException)
            {
                exception = initializationException;
                return false;
            }
        }
    }
}
