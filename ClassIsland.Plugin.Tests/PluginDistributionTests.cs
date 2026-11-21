using System.IO.Compression;
using System.Reflection;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClassIsland.Plugin.Tests;

public sealed class PluginDistributionTests(PluginTestEnvironment environment) : IClassFixture<PluginTestEnvironment>
{
    [Fact]
    public void Distribution_ControlsPluginLoaderAvailability()
    {
        var loader = typeof(PluginService).Assembly.GetType("ClassIsland.PluginLoadContext");
#if CLASSISLAND_APP_STORE
        Assert.False(PluginSupport.IsEnabled);
        Assert.Null(loader);
#else
        Assert.True(PluginSupport.IsEnabled);
        Assert.NotNull(loader);
#endif
    }

    [Fact]
    public void PendingPackage_IsInstalledOnlyWhenPluginsAreEnabled()
    {
        Assert.StartsWith(environment.Root, PluginService.PluginsPkgRootPath, StringComparison.Ordinal);
        Directory.CreateDirectory(PluginService.PluginsPkgRootPath);
        var packagePath = Path.Combine(PluginService.PluginsPkgRootPath, "test.cipx");
        using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create))
        {
            using (var writer = new StreamWriter(archive.CreateEntry("manifest.yml").Open()))
            {
                writer.Write("id: distribution-test\nname: Distribution Test\nversion: 1.0.0\napiVersion: 2.0.0.0\nentranceAssembly: TestPlugin.dll\n");
            }
            archive.CreateEntryFromFile(typeof(PluginDistributionTests).Assembly.Location, "TestPlugin.dll");
        }

        PluginService.ProcessPluginsInstall();

#if CLASSISLAND_APP_STORE
        Assert.True(File.Exists(packagePath));
        Assert.False(Directory.Exists(PluginService.PluginsRootPath));
        PluginService.InitializePlugins(null!, null!);
        Assert.Empty(PluginService.PluginLoadedStatus);
#else
        Assert.False(File.Exists(packagePath));
        Assert.True(File.Exists(Path.Combine(PluginService.PluginsRootPath, "distribution-test", "TestPlugin.dll")));
#endif
    }

#if CLASSISLAND_APP_STORE
    [Fact]
    public async Task MarketOperations_DoNotLoadOrDownloadPlugins()
    {
        var settings = new SettingsService(NullLogger<SettingsService>.Instance, null!);
        var market = new PluginMarketService(settings, new PluginService(), NullLogger<PluginMarketService>.Instance);
        var refreshTime = settings.Settings.LastRefreshPluginSourceTime;

        await market.RefreshPluginSourceAsync();
        market.LoadPluginSource();
        market.UpdateAllPlugins();
        market.RequestDownloadPlugin("distribution-test");

        Assert.False(market.IsLoadingPluginSource);
        Assert.Empty(market.DownloadTasks);
        Assert.Empty(market.Indexes);
        Assert.Empty(market.MergedPlugins);
        Assert.Equal(refreshTime, settings.Settings.LastRefreshPluginSourceTime);
    }
#else
    [Fact]
    public void MonoLoader_LoadsExternalManagedPluginAndSharesHostApi()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"classisland-loader-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            var source = typeof(PluginDistributionTests).Assembly;
            var path = Path.Combine(folder, Path.GetFileName(source.Location));
            File.Copy(source.Location, path);
            var context = new PluginLoadContext(new PluginInfo
            {
                Manifest = new PluginManifest { Id = "distribution-test" }
            }, path, true);

            var assembly = context.LoadFromAssemblyName(source.GetName());
            Assert.NotSame(source, assembly);
            var plugin = Assert.IsAssignableFrom<PluginBase>(
                Activator.CreateInstance(assembly.GetType(typeof(TestPlugin).FullName!)!));
            var services = new ServiceCollection();
            plugin.Initialize(new HostBuilderContext(new Dictionary<object, object>()), services);
            Assert.Contains(services, descriptor => descriptor.ServiceType.FullName == typeof(TestPluginMarker).FullName);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void MonoResolver_MissingAssemblyFallsBackToHost()
    {
        var resolver = new MonoPluginAssemblyResolver(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "plugin.dll"));
        Assert.Null(resolver.ResolveAssemblyToPath(new AssemblyName("Missing.Dependency")));
    }
#endif
}

public sealed class TestPlugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton<TestPluginMarker>();
    }
}

public sealed class TestPluginMarker;

public sealed class PluginTestEnvironment : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), $"classisland-plugins-{Guid.NewGuid():N}");
    private readonly string _previousRoot = CommonDirectories.AppRootFolderPath;

    public PluginTestEnvironment()
    {
        typeof(CommonDirectories).GetProperty(nameof(CommonDirectories.AppRootFolderPath))!.SetValue(null, Root);
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(PluginService).TypeHandle);
    }

    public void Dispose()
    {
        typeof(CommonDirectories).GetProperty(nameof(CommonDirectories.AppRootFolderPath))!.SetValue(null, _previousRoot);
        if (Directory.Exists(Root))
            Directory.Delete(Root, true);
    }
}
