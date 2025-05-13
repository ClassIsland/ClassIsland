using System.Reflection;
using System.Runtime.Loader;
using System;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Services;

namespace ClassIsland;

class PluginLoadContext(PluginInfo info, string fullPath) : AssemblyLoadContext($"ClassIsland.PluginLoadContext[{info.Manifest.Id}]")
{
    public PluginInfo Info { get; } = info;

    private AssemblyDependencyResolver Resolver { get; } = new(fullPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
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
        var assemblyPath = Resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = Resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}