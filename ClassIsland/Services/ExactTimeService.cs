using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using GuerrillaNtp;

using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ClassIsland.Services;

public class ExactTimeService : ObservableRecipient
{
    private string _syncStatusMessage = "时间尚未同步。";

    private DateTime PrevDateTime { get; set; } = DateTime.MinValue;

    private bool NeedWaiting { get; set; } = false;

    private SettingsService SettingsService { get; }

    private NtpClient? NtpClient { get; set; }

    private NtpClock NtpClock { get; set; } = NtpClock.LocalFallback;

    private ILogger<ExactTimeService> Logger { get; }

    private DispatcherTimer UpdateTimer { get; } = new DispatcherTimer()
    {
        Interval = TimeSpan.FromMinutes(10)
    };

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
        Sync();
        UpdateTimerStatus();
        SystemEvents.TimeChanged += SystemEventsOnTimeChanged;

        if (SettingsService.Settings.IsTimeAutoAdjustEnabled)
        {
            SettingsService.Settings.TimeOffsetSeconds += SettingsService.Settings.TimeAutoAdjustSeconds *
                                                          Math.Floor((DateTime.Now - SettingsService.Settings
                                                              .LastTimeAdjustDateTime).TotalDays);
            SettingsService.Settings.TimeOffsetSeconds = Math.Round(SettingsService.Settings.TimeOffsetSeconds, 3);
        }
        SettingsService.Settings.LastTimeAdjustDateTime = DateTime.Now;
    }

    private void SystemEventsOnTimeChanged(object? sender, EventArgs e)
    {
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
            return;

        Logger.LogInformation("正在从 {} 同步时间", SettingsService.Settings.ExactTimeServer);
        SyncStatusMessage = $"正在同步时间……";
        var prev = GetCurrentLocalDateTime();
        try
        {
            NtpClock = NtpClient.Query();
            var now = NtpClock.Now.LocalDateTime;
            if (TimeSpan.FromSeconds(-30) < now - prev && now < prev)
            {
                NeedWaiting = true;
                PrevDateTime = prev;
            }
            Logger.LogInformation("成功地同步了时间，现在是 {}", now);
            SyncStatusMessage = $"成功地在{now}同步了时间";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "同步时间失败。");
            SyncStatusMessage = $"同步时间失败：{ex.Message}";
        }
    }

    public DateTime GetCurrentLocalDateTime()
    {
        var now = SettingsService.Settings.IsExactTimeEnabled ? NtpClock.Now.LocalDateTime : DateTime.Now;
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
        return baseTime + TimeSpan.FromSeconds(SettingsService.Settings.TimeOffsetSeconds);
    }
}