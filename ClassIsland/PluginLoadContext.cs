using System.Reflection;
using System.Runtime.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Services;

namespace ClassIsland;

/// <summary>
/// 为插件加载提供隔离的 <see cref="AssemblyLoadContext"/> 实现。<para/>
/// 根据运行平台选择不同的依赖解析器，并负责从插件目录解析托管与非托管依赖项。
/// <remarks>macOS平台的依赖解析器为<see cref="MacPluginAssemblyResolver"/></remarks>
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly bool _suppressMacPluginLoader;

    public PluginLoadContext(PluginInfo info, string fullPath, bool suppressMacPluginLoader) : base($"ClassIsland.PluginLoadContext[{info.Manifest.Id}]")
    {
        _suppressMacPluginLoader = suppressMacPluginLoader;
        Info = info;
        CoreResolver = UseMacOsPluginLoadingBehavior ? null : new(fullPath);
        MacResolver = UseMacOsPluginLoadingBehavior ? new(fullPath) : null;
    }

    /// <summary>
    /// 插件信息与清单引用，用于根据声明的依赖项查找并委托到其它插件的加载上下文。
    /// </summary>
    public PluginInfo Info { get; }

    private bool UseMacOsPluginLoadingBehavior =>
        _suppressMacPluginLoader || RuntimeInformation.IsOSPlatform(OSPlatform.OSX); 

    private AssemblyDependencyResolver? CoreResolver { get; }
    
    private MacPluginAssemblyResolver? MacResolver { get; }

    private static IReadOnlyList<string> WinRTDeps { get; } = [
        "WinRT.Runtime",
        "Microsoft.Windows.SDK.NET"
    ];

    /// <summary>
    /// 在需要加载程序集时被调用。优先从已加载的插件依赖项上下文中解析，如果在插件目录中找到对应的程序集则从路径加载。
    /// 对某些 WinRT 相关依赖会返回 null 以使用宿主的实现。
    /// </summary>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (WinRTDeps.Contains(assemblyName.Name))
        {
            // 为了防止因引用 WinRT 依赖导致重复初始化 WinRT 相关运行时使应用代码无法正常调用 WinRT，
            // 这里将插件要的加载的 WinRT 相关程序集替换为应用自带的 WinRT 相关程序集。
            return null;
        }
        // 尝试查找依赖
        foreach (var dep in Info.Manifest.Dependencies)
        {
            if (!PluginService.PluginLoadContexts.TryGetValue(dep.Id, out var context))
            {
                continue;
            }

            var assembly = context.Load(assemblyName);
            if (assembly != null)
            {
                return assembly;
            }
        }
        
        string? assemblyPath;
        assemblyPath = UseMacOsPluginLoadingBehavior ? MacResolver?.ResolveAssemblyToPath(assemblyName) : CoreResolver?.ResolveAssemblyToPath(assemblyName);
        
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    /// <summary>
    /// 解析并加载插件的非托管（本地）库，按平台和插件目录的约定搜索文件。
    /// 返回非托管库句柄，找不到则返回 <see cref="IntPtr.Zero"/>。
    /// </summary>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath;
        libraryPath = UseMacOsPluginLoadingBehavior ? MacResolver?.ResolveUnmanagedDllToPath(unmanagedDllName) : CoreResolver?.ResolveUnmanagedDllToPath(unmanagedDllName);

        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}

[SupportedOSPlatform("macos")]
/// <summary>
/// macOS 专用的插件程序集与本地库解析器。根据插件目录结构（包括 runtimes 子目录）查找真实文件路径。
/// </summary>
public class MacPluginAssemblyResolver(string componentAssemblyPath)
{
    private readonly string _pluginDirectory = Path.GetDirectoryName(componentAssemblyPath) ?? "";

    /// <summary>
    /// 将程序集名解析为插件目录下的 dll 文件路径（若存在），否则返回 null。
    /// </summary>
    public string? ResolveAssemblyToPath(AssemblyName assemblyName)
    {
        var dllPath = Path.Combine(_pluginDirectory, assemblyName.Name + ".dll");
        if (File.Exists(dllPath))
            return dllPath;
        return null;
    }

    /// <summary>
    /// 在插件目录及其可能的 runtimes 子目录中查找非托管库文件，并返回第一个匹配的完整路径。
    /// 支持按处理器架构查找文件。
    /// </summary>
    public string? ResolveUnmanagedDllToPath(string unmanagedDllName)
    {
        var searchPaths = new List<string>
        {
            _pluginDirectory,
            Path.Combine(_pluginDirectory, "runtimes", "osx", "native")
        };
        
        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => null
        };
        
        if (arch != null)
        {
            searchPaths.Add(Path.Combine(_pluginDirectory, "runtimes", $"osx-{arch}", "native"));
        }

        foreach (var path in searchPaths)
        {
            if (!Directory.Exists(path)) continue;
            
            var p1 = Path.Combine(path, $"lib{unmanagedDllName}.dylib");
            if (File.Exists(p1)) return p1;
            
            var p2 = Path.Combine(path, $"{unmanagedDllName}.dylib");
            if (File.Exists(p2)) return p2;

            var p3 = Path.Combine(path, unmanagedDllName);
            if (File.Exists(p3)) return p3;
        }

        return null;
    }
}
