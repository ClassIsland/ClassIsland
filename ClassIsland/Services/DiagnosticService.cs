using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Services.Logging;
using ClassIsland.Services.Management;

using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

/// <summary>
/// 应用诊断服务
/// </summary>
public class DiagnosticService(SettingsService settingsService, FileFolderService fileFolderService, 
    ILogger<DiagnosticService> logger,
    AppLogService appLogService)
{
    private static Stopwatch _startupStopwatch = new();

    private FileFolderService FileFolderService { get; } = fileFolderService;

    private AppLogService AppLogService { get; } = appLogService;

    private ILogger<DiagnosticService> Logger { get; } = logger;

    public static string STARTUP_EVENT_NAME = "AppStartup";

    public static long StartupDurationMs { get; set; } = -1;
    private SettingsService SettingsService { get; } = settingsService;

    public string GetDiagnosticInfo()
    {
        var settings = SettingsService.Settings;
        GetDeviceInfo(out var name, out var vendor);
        List<string> loadedPlugin = new();
        foreach (var plugin in PluginService.PluginLoadedStatus)
        {
            if (plugin.Key.LoadStatus is PluginLoadStatus.Loaded) loadedPlugin.Add(plugin.Value.Name + $"({plugin.Value.Id},{plugin.Value.Version})");
        }
        var list = new Dictionary<string, string>
        {
            {"SystemOsVersion",  RuntimeInformation.OSDescription},
            {"SystemOsArch",  RuntimeInformation.OSArchitecture.ToString()},
            {"SystemDeviceName", name},
            {"SystemDeviceVendor", vendor},
            {"AppPackageRoot", CommonDirectories.AppPackageRoot},
            {"AppRoot", CommonDirectories.AppRootFolderPath},
            {"AppCurrentDirectory", Environment.CurrentDirectory},
            {"AppExecutingEntrance", AppBase.ExecutingEntrance},
            {"AppCurrentMemoryUsage", Helpers.StorageSizeHelper.FormatSize((ulong)MemoryWatchDogService.GetMemoryUsage())+$"({MemoryWatchDogService.GetMemoryUsage()} Bytes)"},
            {"AppStartupDurationMs", StartupDurationMs.ToString()},
            {"AppVersion", App.AppVersionLong},
            {"AppSubChannel", AppBase.Current.AppSubChannel},
            {"AppLoadedPlugin",string.Join(", ",loadedPlugin)},
            {"AppIsAssetsTrimmed", AppBase.Current.IsAssetsTrimmed().ToString()},
            {
                nameof(settings.DiagnosticFirstLaunchTime),
                settings.DiagnosticFirstLaunchTime.ToString(CultureInfo.CurrentCulture)
            },
            { nameof(settings.DiagnosticStartupCount), settings.DiagnosticStartupCount.ToString() },
            //{nameof(settings.DiagnosticCrashCount), settings.DiagnosticCrashCount.ToString()},
            //{nameof(settings.DiagnosticLastCrashTime), settings.DiagnosticLastCrashTime.ToString(CultureInfo.CurrentCulture)},
            //{nameof(settings.DiagnosticCrashFreqDay), settings.DiagnosticCrashFreqDay.ToString("F3")},
            {nameof(settings.DiagnosticMemoryKillCount), settings.DiagnosticMemoryKillCount.ToString()},
            {nameof(settings.DiagnosticLastMemoryKillTime), settings.DiagnosticLastMemoryKillTime.ToString(CultureInfo.CurrentCulture)},
            {nameof(settings.DiagnosticMemoryKillFreqDay), settings.DiagnosticMemoryKillFreqDay.ToString("F3")}
        };

        return string.Join(Environment.NewLine, from i in list select $"{i.Key}: {i.Value}");
    }

    public static void BeginStartup()
    {
        _startupStopwatch.Start();
    }

    public static void EndStartup()
    {
        _startupStopwatch.Stop();
        var seconds = _startupStopwatch.Elapsed.TotalSeconds;
        StartupDurationMs = _startupStopwatch.ElapsedMilliseconds;

        var sec = Math.Floor(seconds / 2) * 2;
        var t1 = Math.Min(30, sec);
        var text = $"[{t1}, {t1 + 2})";
        App.GetService<ILogger<DiagnosticService>>().LogInformation("启动共花费 {}ms, {}", StartupDurationMs, text);
    }

    /// <summary>
    /// 导出诊断信息到文件
    /// </summary>
    /// <param name="path">保存位置</param>
    public async Task ExportDiagnosticData(string path)
    {
        try
        {
            var temp = Directory.CreateTempSubdirectory("ClassIslandDiagnosticExport").FullName;
            var logs = string.Join(Environment.NewLine, AppLogService.Logs);
            //await File.WriteAllTextAsync(Path.Combine(temp, "Logs.log"), logs);
            await File.WriteAllTextAsync(Path.Combine(temp, "DiagnosticInfo.txt"), GetDiagnosticInfo());
            File.Copy(Path.Combine(CommonDirectories.AppRootFolderPath, "Settings.json"), Path.Combine(temp, "Settings.json"));
            var profile = App.GetService<IProfileService>().CurrentProfilePath;
            Directory.CreateDirectory(Path.Combine(temp, "Profiles/"));
            Directory.CreateDirectory(Path.Combine(temp, "Management/"));
            Directory.CreateDirectory(Path.Combine(temp, "Config/"));
            Directory.CreateDirectory(Path.Combine(temp, "Logs/"));
            foreach (var file in Directory.GetFiles(ManagementService.ManagementConfigureFolderPath))
            {
                File.Copy(file, Path.Combine(temp, "Management/", Path.GetFileName(file)));
            }
            File.Copy(Path.Combine(CommonDirectories.AppRootFolderPath, "./Profiles", profile), Path.Combine(temp, "Profiles/",  profile));
            FileFolderService.CopyFolder(Path.Combine(CommonDirectories.AppConfigPath), Path.Combine(temp, "Config/"));
            FileFolderService.CopyFolder(Path.Combine(CommonDirectories.AppLogFolderPath), Path.Combine(temp, "Logs/"));

            File.Delete(path);
            await Task.Run(() =>
            {
                ZipFile.CreateFromDirectory(temp, path);
            });
            Directory.Delete(temp, true);
            Process.Start(new ProcessStartInfo()
            {
                FileName = Path.GetDirectoryName(path) ?? "",
                UseShellExecute = true
            });
        }
        catch (Exception e)
        {
            Logger.LogError(e, "无法导出诊断数据。");
            throw;
        }
    }

    /// <summary>
    /// 获取设备信息
    /// </summary>
    /// <param name="name">输出设备名称</param>
    /// <param name="vendor">输出设备提供商</param>
    /// <remarks>对于Windows平台，返回WMI中Win32_ComputerSystemProduct定义的信息<para/>
    /// 对于Linux平台，返回/sys/devices/virtual/dmi/id/下的product_name和sys_vendor文件内容<para/>
    /// 对于macOS平台，返回固定值"Macintosh"和"Apple Inc."<para/>
    /// </remarks>
    public static void GetDeviceInfo(out string name, out string vendor)
    {
        name = "???";
        vendor = "???";
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var moc = new ManagementClass("Win32_ComputerSystemProduct").GetInstances();
                foreach (var mo in moc)
                {
                    name = mo.GetPropertyValue("Name") as string ?? "???";
                    vendor = mo.GetPropertyValue("Vendor") as string ?? "???";
                }
            }

            if (OperatingSystem.IsLinux())
            {
                if(File.Exists("/sys/devices/virtual/dmi/id/product_name")) name=File.ReadAllText("/sys/devices/virtual/dmi/id/product_name").Trim();
                if (File.Exists("/sys/devices/virtual/dmi/id/sys_vendor")) vendor = File.ReadAllText("/sys/devices/virtual/dmi/id/sys_vendor").Trim();
            }

            if (OperatingSystem.IsMacOS())
            {
                vendor = "Apple Inc.";
                name = "Macintosh";
            }
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>
    /// 处理全局未捕获的域异常
    /// </summary>
    public static void ProcessDomainUnhandledException(object sender, UnhandledExceptionEventArgs eventArgs)
    {
        App.IsCrashed = true;
        if (App._isCriticalSafeModeEnabled)  // 教学安全模式
        {
            return;
        }
        
        try
        {
            var app = (App)AppBase.Current;
            Dispatcher.UIThread.Invoke(() =>
            {
                if (eventArgs.ExceptionObject is Exception exception)
                {
                    app?.ProcessUnhandledException(exception, eventArgs.IsTerminating);
                }
            });
        }
        catch
        {
            if (!eventArgs.IsTerminating)
            {
                return;
            }
            ProcessCriticalException(eventArgs.ExceptionObject as Exception);
        }
    }

    /// <summary>
    /// 处理严重异常
    /// </summary>
    /// <param name="ex">异常信息</param>
    public static void ProcessCriticalException(Exception? ex)
    {
        if (App._isCriticalSafeModeEnabled)  // 教学安全模式
        {
            return;
        }

        // CopyException();
        List<PluginInfo> plugins = [];
        if (ex != null)
        {
            plugins = GetPluginsByStacktrace(ex);
        }

        DisableCorruptPlugins(plugins);
        var pluginsWarning = Environment.NewLine + Environment.NewLine +
                             "此问题可能由以下插件引起，请在向 ClassIsland 开发者反馈问题前先向以下插件的开发者反馈此问题：" + Environment.NewLine
                             + string.Join(Environment.NewLine,
                                 plugins.Select(x => $"- {x.Manifest.Name} [{x.Manifest.Id}]"));
        var message = $"""
                       很抱歉，ClassIsland 遇到了无法解决的问题，即将退出。堆栈跟踪信息已复制到剪贴板。点击【确定】将退出应用，点击【取消】将启动调试器。

                       错误信息：{ex?.Message}{(plugins.Count > 0 ? pluginsWarning : "")}

                       如果您要反馈这个问题或求助，请不要只上传本窗口的截图。请查阅事件查看器和日志获取完整的错误信息，并附加在求助信息中。
                       """;
        
        // TODO: 实现对话框
        // var r = MessageBox.Show(message, "ClassIsland", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
        // if (r == DialogResult.Cancel)
        // {
        //     Debugger.Launch();
        // }
        // return;
        //
        // void CopyException()
        // {
        //     try
        //     {
        //         Clipboard.SetDataObject(ex?.ToString() ?? "");
        //     }
        //     catch (Exception e)
        //     {
        //         // ignored
        //     }
        // }
    }

    /// <summary>
    /// 通过堆栈跟踪获取相关插件
    /// </summary>
    /// <param name="exception">堆栈信息</param>
    /// <returns>获取到的插件列表</returns>
    public static List<PluginInfo> GetPluginsByStacktrace(Exception exception)
    {
        var stack = new StackTrace(exception);
        var frames = stack.GetFrames();
        var plugins = new List<PluginInfo>();
        foreach (var frame in frames)
        {
            var declaringTypeAssembly = frame.GetMethod()?.DeclaringType?.Assembly;
            if (declaringTypeAssembly == null)
            {
                continue;
            }
            var context = AssemblyLoadContext.GetLoadContext(declaringTypeAssembly);
            if (context is not PluginLoadContext pluginLoadContext)
            {
                continue;
            }

            if (!plugins.Contains(pluginLoadContext.Info))
            {
                plugins.Add(pluginLoadContext.Info);
            }
        }

        if (exception.InnerException != null)
        {
            plugins.AddRange(GetPluginsByStacktrace(exception.InnerException));
        }

        return plugins;
    }

    /// <summary>
    /// 禁用损坏的插件
    /// </summary>
    /// <param name="plugins">插件列表</param>
    /// <returns>是否禁用成功</returns>
    public static bool DisableCorruptPlugins(List<PluginInfo> plugins)
    {
        var isPluginAutoDisabled = false;
        if (App.AutoDisableCorruptPlugins && plugins.Count > 0)
        {
            foreach (var plugin in plugins)
            {
                try
                {
                    plugin.IsEnabled = false;
                    isPluginAutoDisabled = true;
                }
                catch
                {
                    // ignored
                }
            }
        }

        if (!isPluginAutoDisabled) 
            return isPluginAutoDisabled;
        try
        {
            ((App)AppBase.Current).Settings.CorruptPluginsDisabledLastSession = true;
        }
        catch
        {
            // ignored
        }

        return isPluginAutoDisabled;
    }
}