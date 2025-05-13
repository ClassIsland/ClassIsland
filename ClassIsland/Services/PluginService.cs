using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Models.Plugins;
using ClassIsland.Services.Management;
using ClassIsland.Shared;
using ClassIsland.Shared.Protobuf.AuditEvent;
using ClassIsland.Shared.Protobuf.Enum;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ClassIsland.Services;

public class PluginService : IPluginService
{
    public static readonly string PluginsRootPath = Path.Combine(App.AppRootFolderPath, @"Plugins\");

    public static readonly string PluginsIndexPath = Path.Combine(App.AppConfigPath, "PluginsIndex");

    public static readonly string PluginsPkgRootPath = Path.Combine(App.AppCacheFolderPath, "PluginPackages");


    public static readonly string PluginManifestFileName = "manifest.yml";

    public static readonly string PluginConfigsFolderPath = Path.Combine(App.AppConfigPath, "Plugins");

    internal static readonly Dictionary<string, PluginLoadContext> PluginLoadContexts = new();

    internal static List<PluginManifest> InstalledPlugins { get; } = [];
    
    internal static List<PluginManifest> UninstalledPlugins { get; } = [];

    public static void ProcessPluginsInstall()
    {
        if (!Directory.Exists(PluginsPkgRootPath))
        {
            Directory.CreateDirectory(PluginsPkgRootPath);
        }
        if (!Directory.Exists(PluginsRootPath))
        {
            Directory.CreateDirectory(PluginsRootPath);
        }

        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        foreach (var pkgPath in Directory.EnumerateFiles(PluginsPkgRootPath).Where(x => Path.GetExtension(x) == IPluginService.PluginPackageExtension))
        {
            try
            {
                using var pkg = ZipFile.OpenRead(pkgPath);
                var mf = pkg.GetEntry(PluginManifestFileName);
                if (mf == null)
                    continue;
                var mfText = new StreamReader(mf.Open()).ReadToEnd();
                var manifest = deserializer.Deserialize<PluginManifest>(mfText);
                var targetPath = Path.Combine(PluginsRootPath, manifest.Id);
                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, true);
                }
                Directory.CreateDirectory(targetPath);
                ZipFile.ExtractToDirectory(pkgPath, targetPath);
                InstalledPlugins.Add(manifest);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            File.Delete(pkgPath);
        }
    }

    public static void InitializePlugins(HostBuilderContext context, IServiceCollection services)
    {
        if (!Directory.Exists(PluginsRootPath))
        {
            Directory.CreateDirectory(PluginsRootPath);
        }

        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var pluginDirs = Directory.EnumerateDirectories(PluginsRootPath)
            .Append(App.ApplicationCommand.ExternalPluginPath);
        // 预处理插件信息
        foreach (var pluginDir in pluginDirs)
        {
            if (string.IsNullOrWhiteSpace(pluginDir))
                continue;
            var manifestPath = Path.Combine(pluginDir, PluginManifestFileName);
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            var manifestYaml = File.ReadAllText(manifestPath);
            var manifest = deserializer.Deserialize<PluginManifest?>(manifestYaml);
            if (manifest == null)
            {
                continue;
            }
            var info = new PluginInfo
            {
                Manifest = manifest,
                IsLocal = true,
                PluginFolderPath = Path.GetFullPath(pluginDir),
                RealIconPath = Path.Combine(Path.GetFullPath(pluginDir), manifest.Icon)
            };
            if (info.IsUninstalling)
            {
                Directory.Delete(pluginDir, true);
                UninstalledPlugins.Add(manifest);
                continue;
            }
            if (IPluginService.LoadedPluginsIds.Contains(manifest.Id))
                continue;
            IPluginService.LoadedPluginsIds.Add(manifest.Id);
            IPluginService.LoadedPluginsInternal.Add(info);
            if (!info.IsEnabled)
            {
                info.LoadStatus = PluginLoadStatus.Disabled;
            }
        }
        var loadOrder = ResolveLoadOrder(IPluginService.LoadedPluginsInternal.Where(x => x.LoadStatus == PluginLoadStatus.NotLoaded).ToList());
        Console.WriteLine($"Resolved load order: {string.Join(", ", loadOrder)}");

        // 加载插件
        foreach (var id in loadOrder)
        {
            var info = IPluginService.LoadedPluginsInternal.First(x => x.Manifest.Id == id);
            var manifest = info.Manifest;
            var pluginDir = info.PluginFolderPath;
            try
            {
                var fullPath = Path.GetFullPath(Path.Combine(pluginDir, manifest.EntranceAssembly));
                var loadContext = new PluginLoadContext(info, fullPath);
                PluginLoadContexts[info.Manifest.Id] = loadContext;
                var asm = loadContext.LoadFromAssemblyName(
                    new AssemblyName(Path.GetFileNameWithoutExtension(fullPath)));
                var entrance = asm.ExportedTypes.FirstOrDefault(x =>
                    x.BaseType == typeof(PluginBase) ||
                    x.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(PluginEntrance)) != null);

                if (entrance == null)
                {
                    continue;
                }

                if (Activator.CreateInstance(entrance) is not PluginBase entranceObj)
                {
                    continue;
                }

                entranceObj.PluginConfigFolder = Path.Combine(PluginConfigsFolderPath, manifest.Id);
                if (!Directory.Exists(entranceObj.PluginConfigFolder))
                    Directory.CreateDirectory(entranceObj.PluginConfigFolder);
                entranceObj.Info = info;
                entranceObj.Initialize(context, services);
                services.AddSingleton(typeof(PluginBase), entranceObj);
                services.AddSingleton(entrance, entranceObj);
                info.LoadStatus = PluginLoadStatus.Loaded;
                Console.WriteLine($"Initialize plugin: {pluginDir} ({manifest.Version})");
            }
            catch (Exception ex)
            {
                info.Exception = ex;
                info.LoadStatus = PluginLoadStatus.Error;
            }
        }
        
        AppBase.Current.AppStarted += CurrentOnAppStarted;
    }

    private static void CurrentOnAppStarted(object? sender, EventArgs e)
    {
        if (IAppHost.TryGetService<IManagementService>() is not
            { IsManagementEnabled: true, Connection: ManagementServerConnection connection })
        {
            return;
        }

        foreach (var i in InstalledPlugins)
        {
            connection.LogAuditEvent(AuditEvents.PluginInstalled, new PluginInstalled()
            {
                PluginId = i.Id,
                Version = i.Version
            });
        }
        foreach (var i in UninstalledPlugins)
        {
            connection.LogAuditEvent(AuditEvents.PluginUninstalled, new PluginUninstalled()
            {
                PluginId = i.Id,
                Version = i.Version
            });
        }
    }

    public static async Task PackagePluginAsync(string id, string outputPath)
    {
        var plugin = IPluginService.LoadedPlugins.FirstOrDefault(x => x.Manifest.Id == id);
        if (plugin == null)
        {
            throw new ArgumentException($"找不到插件 {id}。", nameof(id));
        }

        await Task.Run(() =>
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            ZipFile.CreateFromDirectory(plugin.PluginFolderPath, outputPath);
        });
    }

    private static List<string> ResolveLoadOrder(List<PluginInfo> plugins)
    {
        var nodes = plugins
            .Where(x => x.LoadStatus == PluginLoadStatus.NotLoaded)
            .ToDictionary(
            x => x.Manifest.Id, 
            x => new DependencyNode(x));
        foreach (var i in nodes)
        {
            ResolveDependencyNode(nodes, i.Value, []);
        }
        return nodes
            .Where(x => x.Value.Plugin.LoadStatus == PluginLoadStatus.NotLoaded)
            .OrderBy(x => x.Value.DependencyTreeDepth)
            .Select(x => x.Key)
            .ToList();
    }

    private static void ResolveDependencyNode(Dictionary<string, DependencyNode> allNodes, DependencyNode node, List<DependencyNode> walkingNodes)
    {
        if (node.IsDiscovered)
        {
            return;
        }

        if (walkingNodes.Contains(node))
        {
            throw new InvalidOperationException(
                $"检测到循环依赖：{string.Join(" -> ", walkingNodes.Select(x => x.Plugin.Manifest.Id))}");
        }

        node.IsDiscovered = true;
        var depth = 0;
        foreach (var i in node.Plugin.Manifest.Dependencies)
        {
            if (!allNodes.TryGetValue(i.Id, out var dependency) || dependency.Plugin.LoadStatus != PluginLoadStatus.NotLoaded)
            {
                if (i.IsRequired)
                {
                    node.Plugin.LoadStatus = PluginLoadStatus.Error;
                    node.Plugin.Exception = new InvalidOperationException($"插件 {node.Plugin.Manifest.Id} 依赖的必选插件 {i.Id} 不存在或处于无法加载状态。");
                    return;
                }
                continue;
            }

            ResolveDependencyNode(allNodes, dependency, walkingNodes);
            depth = Math.Max(depth, dependency.DependencyTreeDepth);
        }
        node.DependencyTreeDepth = depth + 1;

    }
}