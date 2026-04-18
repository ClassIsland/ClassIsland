using System;
using System.ComponentModel;
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
        using var root = Registry.CurrentUser.OpenSubKey($@"{ConsentStoreBasePath}\{capabilityName}");
        if (root == null)
        {
            return false;
        }

        return ContainsActiveUsage(root, 4);
    }

    private static bool ContainsActiveUsage(RegistryKey key, int depth)
    {
        if (IsUsageActive(key))
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

            if (ContainsActiveUsage(subKey, depth - 1))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsUsageActive(RegistryKey key)
    {
        var start = ReadFileTimeValue(key, "LastUsedTimeStart");
        if (start <= 0)
        {
            return false;
        }

        var stop = ReadFileTimeValue(key, "LastUsedTimeStop");
        return stop <= 0 || stop < start;
    }

    private static long ReadFileTimeValue(RegistryKey key, string valueName)
    {
        var value = key.GetValue(valueName);
        return value switch
        {
            long l => l,
            int i => i,
            byte[] bytes when bytes.Length >= 8 => BitConverter.ToInt64(bytes, 0),
            string s when long.TryParse(s, out var parsed) => parsed,
            _ => 0
        };
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= TimerOnTick;
    }
}
