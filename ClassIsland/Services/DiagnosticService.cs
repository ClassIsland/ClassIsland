using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.AppCenter.Analytics;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class DiagnosticService(SettingsService settingsService)
{
    private static Stopwatch _startupStopwatch = new();

    public static string STARTUP_EVENT_NAME = "AppStartup";

    public static long StartupDurationMs { get; set; } = -1;
    private SettingsService SettingsService { get; } = settingsService;

    public string GetDiagnosticInfo()
    {
        var settings = SettingsService.Settings;
        var list = new Dictionary<string, string>
        {
            {"SystemOsVersion",  RuntimeInformation.OSDescription},
            {"SystemIsCompositionEnabled", NativeWindowHelper.DwmIsCompositionEnabled().ToString()},
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
}