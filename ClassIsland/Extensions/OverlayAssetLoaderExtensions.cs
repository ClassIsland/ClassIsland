using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Platform;
using ClassIsland.Core;

namespace ClassIsland.Extensions;

public static class OverlayAssetLoaderExtensions
{
    /// <summary>
    /// 在应用初始化前绑定资源重写加载器。可保留配置回调中的实例，以便运行时继续注册重写。
    /// </summary>
    public static AppBuilder UseOverlayAssetLoader(
        this AppBuilder builder, Action<OverlayAssetLoader>? configure = null)
    {
        return builder.AfterPlatformServicesSetup(appBuilder =>
            BindAssetLoader(appBuilder.ApplicationType?.Assembly ?? typeof(App).Assembly, configure));
    }

    // Avalonia 的引用程序集隐藏了定位器成员；保留运行时反射访问所需的成员，兼容 Android 裁剪。
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods,
        typeof(AvaloniaLocator))]
    private static void BindAssetLoader(Assembly assembly, Action<OverlayAssetLoader>? configure)
    {
        const BindingFlags flags = BindingFlags.Public
                                   | BindingFlags.NonPublic
                                   | BindingFlags.Static
                                   | BindingFlags.Instance;

        var locatorType = typeof(AvaloniaLocator);
        var resolver = locatorType.GetProperty("Current", flags)?.GetValue(null)
                       ?? throw new InvalidOperationException("Unable to get AvaloniaLocator.Current.");
        var resolverType = Type.GetType("Avalonia.IAvaloniaDependencyResolver, Avalonia.Base")
                           ?? throw new InvalidOperationException("Unable to get Avalonia dependency resolver type.");
        var fallback = resolverType.GetMethod("GetService")?.Invoke(resolver, [typeof(IAssetLoader)]) as IAssetLoader
                       ?? throw new InvalidOperationException("Unable to get Avalonia IAssetLoader.");

        var overlay = fallback as OverlayAssetLoader ?? new OverlayAssetLoader(fallback, assembly);
        overlay.RegisterOverride(OpenBlobAsset, assembly);
        configure?.Invoke(overlay);

        var locator = locatorType.GetProperty("CurrentMutable", flags)?.GetValue(null)
                      ?? throw new InvalidOperationException("Unable to get AvaloniaLocator.CurrentMutable.");
        var bindMethod = locatorType.GetMethod("Bind", flags)?.MakeGenericMethod(typeof(IAssetLoader))
                         ?? throw new InvalidOperationException("Unable to get AvaloniaLocator.Bind<T>().");
        var registration = bindMethod.Invoke(locator, null)
                           ?? throw new InvalidOperationException("Unable to bind Avalonia IAssetLoader.");
        var registrationType = Type.GetType("Avalonia.AvaloniaLocator+RegistrationHelper`1, Avalonia.Base")
                                   ?.MakeGenericType(typeof(IAssetLoader))
                               ?? throw new InvalidOperationException("Unable to get Avalonia registration type.");
        var toConstantMethod = registrationType.GetMethod("ToConstant")?.MakeGenericMethod(typeof(IAssetLoader))
                               ?? throw new InvalidOperationException("Unable to get AvaloniaLocator.ToConstant<T>().");

        toConstantMethod.Invoke(registration, [overlay]);
    }

    private static Stream? OpenBlobAsset(Uri uri)
    {
        const string prefix = "/sha256/";
        if (!uri.Scheme.Equals("avares", StringComparison.OrdinalIgnoreCase)
            || !uri.Authority.Equals("*blobs", StringComparison.Ordinal)
            || !uri.AbsolutePath.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var hash = uri.AbsolutePath[prefix.Length..];
        if (hash.Length != 64 || !hash.All(Uri.IsHexDigit))
        {
            return null;
        }

        hash = hash.ToLowerInvariant();
        var path = Path.Combine(CommonDirectories.AppBlobFolderPath, hash[..2], hash);
        return File.Exists(path) ? File.OpenRead(path) : null;
    }
}
