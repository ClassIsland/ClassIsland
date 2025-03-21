using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared.Abstraction.Services;
using CommunityToolkit.Mvvm.ComponentModel;

using GuerrillaNtp;

using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ClassIsland.Services;

public class ExactTimeService : ObservableRecipient, IExactTimeService
{
    private string _syncStatusMessage = "时间尚未同步。";

    private DateTime PrevDateTime { get; set; } = DateTime.MinValue;

    private bool NeedWaiting { get; set; } = false;

    private SettingsService SettingsService { get; }

    private NtpClient? NtpClient { get; set; }

    private NtpClock NtpClock { get; set; } = NtpClock.LocalFallback;

    private DateTime LastTime { get; set; } = DateTime.Now;

    private bool WaitingForSystemTimeChanged { get; set; } = false;

    private DateTime LastSystemTime { get; set; } = DateTime.Now;

    private Stopwatch TimeGetStopwatch { get; } = Stopwatch.StartNew();

    private ILogger<ExactTimeService> Logger { get; }

    private DispatcherTimer UpdateTimer { get; } = new DispatcherTimer()
    {
        Interval = TimeSpan.FromMinutes(10)
    };

    private bool _isSyncingTime = false;

    public string SyncStatusMessage
    {
        get => _syncStatusMessage;
        set
        {
            if (value == _syncStatusMessage) return;
            _syncStatusMessage = value;
            OnPropertyChanged();
        }
    }

    public ExactTimeService(SettingsService settingsService, ILogger<ExactTimeService> logger)
    {
        Logger = logger;
        SettingsService = settingsService;
        SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
        UpdateTimer.Tick += (sender, args) =>
        {
            _ = Task.Run(Sync);
        };
        try
        {
            NtpClient = new NtpClient(SettingsService.Settings.ExactTimeServer);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "初始化NtpClient失败。");
            SyncStatusMessage = ex.Message;
        }
        Task.Run(() => {
            Sync();
            UpdateTimerStatus();
            SystemEvents.TimeChanged += SystemEventsOnTimeChanged;
            AppBase.Current.AppStopping += (sender, args) => SystemEvents.TimeChanged -= SystemEventsOnTimeChanged; ;
        });

        if (SettingsService.Settings.IsTimeAutoAdjustEnabled)
        {
            SettingsService.Settings.TimeOffsetSeconds += SettingsService.Settings.TimeAutoAdjustSeconds *
                                                          Math.Floor((DateTime.Now.Date - SettingsService.Settings
                                                              .LastTimeAdjustDateTime.Date).TotalDays);
            SettingsService.Settings.TimeOffsetSeconds = Math.Round(SettingsService.Settings.TimeOffsetSeconds, 3);
        }
        SettingsService.Settings.LastTimeAdjustDateTime = DateTime.Now.Date;
    }

    private void SystemEventsOnTimeChanged(object? sender, EventArgs e)
    {
        WaitingForSystemTimeChanged = true;
        Logger.LogInformation("系统时间修改，正在重新同步时间");
        Task.Run(Sync);
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SettingsService.Settings.ExactTimeServer):

                try
                {
                    NtpClient = new NtpClient(SettingsService.Settings.ExactTimeServer);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "初始化NtpClient失败。");
                    SyncStatusMessage = ex.Message;
                }
                break;
            case nameof(SettingsService.Settings.IsExactTimeEnabled):
                UpdateTimerStatus();
                break;
        }
    }

    private void UpdateTimerStatus()
    {
        if (SettingsService.Settings.IsExactTimeEnabled)
        {
            UpdateTimer.Start();
        }
        else
        {
            UpdateTimer.Stop();
        }
    }

    public void Sync()
    {
        if (!SettingsService.Settings.IsExactTimeEnabled || NtpClient == null)
        {
            WaitingForSystemTimeChanged = false;
            return;
        }

        if (_isSyncingTime)
        {
            return;
        }
        _isSyncingTime = true;

        Logger.LogInformation("正在从 {} 同步时间", SettingsService.Settings.ExactTimeServer);
        SyncStatusMessage = $"正在同步时间……";
        var prev = SettingsService.Settings.IsExactTimeEnabled ? NtpClock.Now.LocalDateTime : DateTime.Now;
        try
        {
            NtpClock = NtpClient.Query();
            var nowBase = SettingsService.Settings.IsExactTimeEnabled ? NtpClock.Now.LocalDateTime : DateTime.Now;
            if (Math.Abs((nowBase - prev).TotalSeconds) < 30 && nowBase < prev)
            {
                NeedWaiting = true;
                PrevDateTime = prev;
            }

            Logger.LogInformation("成功地同步了时间，现在是 {}", nowBase);
            SyncStatusMessage = $"成功地在{nowBase}同步了时间";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "同步时间失败。");
            SyncStatusMessage = $"同步时间失败：{ex.Message}";
        }
        finally
        {
            LastSystemTime = DateTime.Now;
            TimeGetStopwatch.Restart();
            WaitingForSystemTimeChanged = false;
            _isSyncingTime = false;
        }
    }

    public DateTime GetCurrentLocalDateTime()
    {
        var systemTime = DateTime.Now;
        if (SettingsService.Settings.IsExactTimeEnabled)
        {
            if (Math.Abs((LastSystemTime - systemTime).TotalMilliseconds - TimeGetStopwatch.ElapsedMilliseconds) > 30_000.0 && !WaitingForSystemTimeChanged)
            {
                WaitingForSystemTimeChanged = true;
                Logger.LogInformation("检测到系统时间突变，已暂停返回的时间并重新同步时间");
                Task.Run(Sync);
            }
            if (WaitingForSystemTimeChanged)
            {
                return LastTime;
            }
        }

        LastSystemTime = systemTime;
        TimeGetStopwatch.Restart();
        var now = SettingsService.Settings.IsExactTimeEnabled ? NtpClock.Now.LocalDateTime : systemTime;
        DateTime baseTime;
        if (now < PrevDateTime && NeedWaiting)
        {
            baseTime = PrevDateTime;
        }
        else
        {
            if (NeedWaiting)
                NeedWaiting = false;
            baseTime = now;
        }
        return LastTime = baseTime + TimeSpan.FromSeconds(SettingsService.Settings.TimeOffsetSeconds +
                                                       SettingsService.Settings.DebugTimeOffsetSeconds);
    }
}