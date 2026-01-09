using System.Reflection;
using System.Runtime.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Services;

namespace ClassIsland;

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

    public PluginInfo Info { get; }

    private bool UseMacOsPluginLoadingBehavior =>
        _suppressMacPluginLoader || RuntimeInformation.IsOSPlatform(OSPlatform.OSX); 

    private AssemblyDependencyResolver? CoreResolver { get; }
    
    private MacPluginAssemblyResolver? MacResolver { get; }

    private static IReadOnlyList<string> WinRTDeps { get; } = [
        "WinRT.Runtime",
        "Microsoft.Windows.SDK.NET"
    ];

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

public class MacPluginAssemblyResolver(string componentAssemblyPath)
{
    private readonly string _pluginDirectory = Path.GetDirectoryName(componentAssemblyPath) ?? "";

    public string? ResolveAssemblyToPath(AssemblyName assemblyName)
    {
        var dllPath = Path.Combine(_pluginDirectory, assemblyName.Name + ".dll");
        if (File.Exists(dllPath))
            return dllPath;
        return null;
    }

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
