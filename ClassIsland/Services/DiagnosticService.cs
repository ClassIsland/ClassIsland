using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using ClassIsland.Services.Logging;
using ClassIsland.Services.Management;

using Microsoft.AppCenter.Analytics;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

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
        DwmIsCompositionEnabled(out BOOL isCompositionEnabled);
        var list = new Dictionary<string, string>
        {
            {"SystemOsVersion",  RuntimeInformation.OSDescription},
            {"SystemIsCompositionEnabled", isCompositionEnabled.ToString()},
            {"AppCurrentMemoryUsage", Process.GetCurrentProcess().PrivateMemorySize64.ToString("N")},
            {"AppStartupDurationMs", StartupDurationMs.ToString()},
            {"AppVersion", App.AppVersionLong},
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

        return string.Join('\n', from i in list select $"{i.Key}: {i.Value}");
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
        Analytics.TrackEvent(STARTUP_EVENT_NAME, new Dictionary<string, string>()
        {
            {"Duration", text}
        });
    }

    public async Task ExportDiagnosticData(string path)
    {
        try
        {
            var temp = Directory.CreateTempSubdirectory("ClassIslandDiagnosticExport").FullName;
            var logs = string.Join('\n', AppLogService.Logs);
            await File.WriteAllTextAsync(Path.Combine(temp, "Logs.log"), logs);
            await File.WriteAllTextAsync(Path.Combine(temp, "DiagnosticInfo.txt"), GetDiagnosticInfo());
            File.Copy("./Settings.json", Path.Combine(temp, "Settings.json"));
            var profile = App.GetService<ProfileService>().CurrentProfilePath;
            Directory.CreateDirectory(Path.Combine(temp, "Profiles/"));
            Directory.CreateDirectory(Path.Combine(temp, "Management/"));
            foreach (var file in Directory.GetFiles(ManagementService.ManagementConfigureFolderPath))
            {
                File.Copy(file, Path.Combine(temp, "Management/", Path.GetFileName(file)));
            }
            File.Copy(Path.Combine("./Profiles", profile), Path.Combine(temp, "Profiles/",  profile));

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
}