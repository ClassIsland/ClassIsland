using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Models.Plugin;
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
                continue;
            }
            if (IPluginService.LoadedPluginsIds.Contains(manifest.Id))
                continue;
            IPluginService.LoadedPluginsIds.Add(manifest.Id);
            IPluginService.LoadedPluginsInternal.Add(info);
            if (!info.IsEnabled)
            {
                info.LoadStatus = PluginLoadStatus.Disabled;
                continue;
            }

            try
            {
                var fullPath = Path.GetFullPath(Path.Combine(pluginDir, manifest.EntranceAssembly));
                var loadContext = new PluginLoadContext(fullPath);
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
}