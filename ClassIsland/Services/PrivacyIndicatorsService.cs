using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Threading;
using ClassIsland.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ClassIsland.Services;

public interface IPrivacyIndicatorsService : INotifyPropertyChanged
{
    PrivacyIndicatorState CurrentState { get; }
    bool IsSupported { get; }
}

public partial class PrivacyIndicatorsService : ObservableRecipient, IPrivacyIndicatorsService, IDisposable
{
    private const string ConsentStoreBasePath = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore";
    private const int CapabilitySearchDepth = 6;
    private static readonly long EqualStartStopActiveWindowTicks = TimeSpan.FromMinutes(3).Ticks;

    private readonly DispatcherTimer _timer = new()
    {
        Interval = TimeSpan.FromSeconds(1)
    };

    private readonly ILogger<PrivacyIndicatorsService> _logger;

    [ObservableProperty] private PrivacyIndicatorState _currentState = PrivacyIndicatorState.None;

    public bool IsSupported => OperatingSystem.IsWindows();

    public PrivacyIndicatorsService(ILogger<PrivacyIndicatorsService> logger)
    {
        _logger = logger;
        _timer.Tick += TimerOnTick;
        _timer.Start();
        RefreshState();
    }

    private void TimerOnTick(object? sender, EventArgs e)
    {
        RefreshState();
    }

    private void RefreshState()
    {
        if (!IsSupported)
        {
            CurrentState = PrivacyIndicatorState.None;
            return;
        }

        try
        {
            var isCameraInUse = IsCapabilityInUse("webcam");
            var isMicrophoneInUse = IsCapabilityInUse("microphone");
            var isLocationInUse = IsCapabilityInUse("location");

            CurrentState = isCameraInUse
                ? PrivacyIndicatorState.Camera
                : isMicrophoneInUse
                    ? PrivacyIndicatorState.Microphone
                    : isLocationInUse
                        ? PrivacyIndicatorState.Location
                        : PrivacyIndicatorState.None;
        }
        catch (Exception ex)
        {
            CurrentState = PrivacyIndicatorState.None;
            _logger.LogDebug(ex, "Failed to refresh privacy indicator state.");
        }
    }

    private static bool IsCapabilityInUse(string capabilityName)
    {
        return IsCapabilityInUseFromHive(Registry.CurrentUser, capabilityName) ||
               IsCapabilityInUseFromHive(Registry.LocalMachine, capabilityName);
    }

    private static bool IsCapabilityInUseFromHive(RegistryKey hive, string capabilityName)
    {
        try
        {
            using var root = hive.OpenSubKey($@"{ConsentStoreBasePath}\{capabilityName}");
            if (root == null)
            {
                return false;
            }

            return ContainsActiveUsage(root, root.Name, CapabilitySearchDepth);
        }
        catch
        {
            return false;
        }
    }

    private static bool ContainsActiveUsage(RegistryKey key, string capabilityRootPath, int depth)
    {
        if (ShouldEvaluateUsageState(key, capabilityRootPath) && IsUsageActive(key))
        {
            return true;
        }

        if (depth <= 0)
        {
            return false;
        }

        foreach (var subKeyName in key.GetSubKeyNames())
        {
            using var subKey = key.OpenSubKey(subKeyName);
            if (subKey == null)
            {
                continue;
            }

            if (ContainsActiveUsage(subKey, capabilityRootPath, depth - 1))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldEvaluateUsageState(RegistryKey key, string capabilityRootPath)
    {
        // Skip container nodes; only leaf-like app nodes should drive active state.
        if (string.Equals(key.Name, capabilityRootPath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (key.Name.EndsWith(@"\NonPackaged", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool IsUsageActive(RegistryKey key)
    {
        var start = ReadFileTimeValue(key, "LastUsedTimeStart");
        if (start <= 0)
        {
            return false;
        }

        var stop = ReadFileTimeValue(key, "LastUsedTimeStop");
        if (stop <= 0 || stop == long.MaxValue)
        {
            return true;
        }

        if (stop < start)
        {
            return true;
        }

        if (stop == start)
        {
            // Some providers report start==stop during a short active transition.
            // Keep this window tight to avoid stale-history false positives.
            var nowFileTime = DateTime.UtcNow.ToFileTimeUtc();
            return nowFileTime >= start && nowFileTime - start <= EqualStartStopActiveWindowTicks;
        }

        return false;
    }

    private static long ReadFileTimeValue(RegistryKey key, string valueName)
    {
        var value = key.GetValue(valueName);
        return value switch
        {
            long l => l,
            ulong ul => NormalizeUnsignedFileTime(ul),
            int i => i,
            uint ui => ui,
            byte[] bytes when bytes.Length >= 8 => NormalizeUnsignedFileTime(BitConverter.ToUInt64(bytes, 0)),
            string s when TryParseFileTimeString(s, out var parsed) => parsed,
            _ => 0
        };
    }

    private static long NormalizeUnsignedFileTime(ulong value)
    {
        return value > long.MaxValue ? long.MaxValue : (long)value;
    }

    private static bool TryParseFileTimeString(string value, out long parsed)
    {
        var raw = value.Trim();
        if (raw.Length == 0)
        {
            parsed = 0;
            return false;
        }

        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            parsed = longValue;
            return true;
        }

        if (ulong.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ulongValue))
        {
            parsed = NormalizeUnsignedFileTime(ulongValue);
            return true;
        }

        if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
            ulong.TryParse(raw[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue))
        {
            parsed = NormalizeUnsignedFileTime(hexValue);
            return true;
        }

        parsed = 0;
        return false;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= TimerOnTick;
    }
}
