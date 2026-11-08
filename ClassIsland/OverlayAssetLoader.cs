using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Platform;

namespace ClassIsland;

public sealed class OverlayAssetLoader(
    IAssetLoader fallback,
    Assembly localAssembly)
    : IAssetLoader
{
    private readonly object _overridesLock = new();
    private readonly List<AssetOverride> _overrides = [];

    public OverlayAssetLoader(
        IAssetLoader fallback,
        Assembly localAssembly,
        string assemblyName,
        string avaresPrefix,
        string physicalRoot) : this(fallback, localAssembly)
    {
        RegisterDirectoryOverride(localAssembly, assemblyName, avaresPrefix, physicalRoot);
    }

    /// <summary>
    /// 追加资源重写，在原始加载器未命中时按注册顺序尝试。
    /// </summary>
    /// <param name="open">
    /// 接收绝对 URI，未命中时返回 null；命中时返回由调用方释放的独立流。
    /// Exists 和 GetAssembly 也会调用此回调，并立即释放探测流。回调异常直接传播。
    /// </param>
    /// <param name="assembly">命中资源关联的程序集，默认为构造时指定的程序集。</param>
    /// <param name="getAssets">可选的目录枚举回调，接收绝对 URI。</param>
    public void RegisterOverride(
        Func<Uri, Stream?> open,
        Assembly? assembly = null,
        Func<Uri, IEnumerable<Uri>>? getAssets = null)
    {
        ArgumentNullException.ThrowIfNull(open);

        lock (_overridesLock)
        {
            _overrides.Add(new AssetOverride(open, assembly ?? localAssembly, getAssets));
        }
    }

    /// <summary>
    /// 将指定程序集的资源前缀映射到文件目录，并提供递归资源枚举。
    /// </summary>
    public void RegisterDirectoryOverride(Assembly assembly, string avaresPrefix, string physicalRoot)
    {
        RegisterDirectoryOverride(assembly, assembly.GetName().Name!, avaresPrefix, physicalRoot);
    }

    public void SetDefaultAssembly(Assembly assembly)
    {
        fallback.SetDefaultAssembly(assembly);
    }

    public bool Exists(Uri uri, Uri? baseUri = null)
    {
        if (fallback.Exists(uri, baseUri))
            return true;

        using var stream = OpenOverride(EnsureAbsolute(uri, baseUri), out _);
        return stream != null;
    }

    public Stream Open(Uri uri, Uri? baseUri = null)
    {
        if (fallback.Exists(uri, baseUri))
            return fallback.Open(uri, baseUri);

        return OpenOverride(EnsureAbsolute(uri, baseUri), out _) ?? fallback.Open(uri, baseUri);
    }

    public (Stream stream, Assembly assembly) OpenAndGetAssembly(Uri uri, Uri? baseUri = null)
    {
        if (fallback.Exists(uri, baseUri))
            return fallback.OpenAndGetAssembly(uri, baseUri);

        var stream = OpenOverride(EnsureAbsolute(uri, baseUri), out var assembly);
        if (stream != null)
            return (stream, assembly!);

        return fallback.OpenAndGetAssembly(uri, baseUri);
    }

    public Assembly? GetAssembly(Uri uri, Uri? baseUri = null)
    {
        if (fallback.Exists(uri, baseUri))
            return fallback.GetAssembly(uri, baseUri);

        using var stream = OpenOverride(EnsureAbsolute(uri, baseUri), out var assembly);
        if (stream != null)
            return assembly;

        return fallback.GetAssembly(uri, baseUri);
    }

    public IEnumerable<Uri> GetAssets(Uri uri, Uri? baseUri)
    {
        var absolute = EnsureAbsolute(uri, baseUri);

        var overrides = GetOverrides();
        return fallback.GetAssets(uri, baseUri)
            .Concat(overrides
                .Where(assetOverride => assetOverride.GetAssets != null)
                .SelectMany(assetOverride => assetOverride.GetAssets!(absolute)))
            .Distinct();
    }

    public void InvalidateAssemblyCache(string name)
    {
        fallback.InvalidateAssemblyCache(name);
    }

    public void InvalidateAssemblyCache()
    {
        fallback.InvalidateAssemblyCache();
    }

    private Stream? OpenOverride(Uri absolute, out Assembly? assembly)
    {
        foreach (var assetOverride in GetOverrides())
        {
            var stream = assetOverride.Open(absolute);
            if (stream == null)
                continue;

            assembly = assetOverride.Assembly;
            return stream;
        }

        assembly = null;
        return null;
    }

    private AssetOverride[] GetOverrides()
    {
        lock (_overridesLock)
        {
            return _overrides.ToArray();
        }
    }

    private void RegisterDirectoryOverride(
        Assembly assembly, string assemblyName, string avaresPrefix, string physicalRoot)
    {
        var prefix = NormalizeAvaresPrefix(avaresPrefix);
        var root = Path.GetFullPath(physicalRoot);

        bool IsHandled(Uri uri) =>
            uri.Scheme.Equals("avares", StringComparison.OrdinalIgnoreCase)
            && uri.Authority.Equals(assemblyName, StringComparison.Ordinal)
            && Uri.UnescapeDataString(uri.AbsolutePath).StartsWith(prefix, StringComparison.Ordinal);

        string MapPath(Uri uri) => SafeCombine(root,
            Uri.UnescapeDataString(uri.AbsolutePath)[prefix.Length..]
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar));

        Stream? OpenFile(Uri uri)
        {
            if (!IsHandled(uri))
                return null;

            var filePath = MapPath(uri);
            return File.Exists(filePath) ? File.OpenRead(filePath) : null;
        }

        IEnumerable<Uri> GetFiles(Uri uri)
        {
            if (!IsHandled(uri))
                return [];

            var directory = MapPath(uri);
            if (!Directory.Exists(directory))
                return [];

            return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                .Select(file =>
                {
                    var relative = Path.GetRelativePath(root, file)
                        .Replace(Path.DirectorySeparatorChar, '/')
                        .Replace(Path.AltDirectorySeparatorChar, '/');

                    var escaped = string.Join('/', relative.Split('/').Select(Uri.EscapeDataString));
                    return new Uri($"avares://{assemblyName}{prefix}{escaped}");
                });
        }

        RegisterOverride(OpenFile, assembly, GetFiles);
    }

    private static Uri EnsureAbsolute(Uri uri, Uri? baseUri)
    {
        if (uri.IsAbsoluteUri)
            return uri;

        if (baseUri is null)
            throw new InvalidOperationException($"Relative asset URI '{uri}' requires base URI.");

        return new Uri(baseUri, uri);
    }

    private static string SafeCombine(string root, string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        var rootWithSlash = root.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!fullPath.StartsWith(rootWithSlash, comparison)
            && !string.Equals(fullPath, root, comparison))
        {
            throw new InvalidOperationException($"Asset path escapes root directory: {relativePath}");
        }

        return fullPath;
    }

    private static string NormalizeAvaresPrefix(string prefix)
    {
        prefix = prefix.Replace('\\', '/');

        if (!prefix.StartsWith('/'))
            prefix = "/" + prefix;

        if (!prefix.EndsWith('/'))
            prefix += "/";

        return prefix;
    }

    private sealed record AssetOverride(
        Func<Uri, Stream?> Open,
        Assembly Assembly,
        Func<Uri, IEnumerable<Uri>>? GetAssets);
}
