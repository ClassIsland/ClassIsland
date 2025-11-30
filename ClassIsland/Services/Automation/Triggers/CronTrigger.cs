using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.Automation.Triggers;
using Microsoft.Extensions.Logging;
using TimeCrontab;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.cron", "cron", "\ue125")]
public class CronTrigger : TriggerBase<CronTriggerSettings>
{
    private Crontab? _crontab;

    private CancellationTokenSource? _stopCancellationTokenSource;

    ILogger<CronTrigger> Logger { get; } = App.GetService<ILogger<CronTrigger>>();
    IExactTimeService ExactTimeService { get; } = App.GetService<IExactTimeService>();

    public override void Loaded()
    {
        Settings.PropertyChanged += SettingsOnPropertyChanged;
        _stopCancellationTokenSource = new CancellationTokenSource();
        LoadCron();
    }

    private async Task CronWorker()
    {
        while (_stopCancellationTokenSource?.IsCancellationRequested == false)
        {
            if (_crontab == null)
            {
                continue;
            }

            var now = ExactTimeService.GetCurrentLocalDateTime();
            await Task.Delay(_crontab.GetSleepTimeSpan(now), _stopCancellationTokenSource.Token);
            if (_stopCancellationTokenSource.IsCancellationRequested)
            {
                break;
            }

            Dispatcher.UIThread.Invoke(Trigger);
        }
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        LoadCron();
    }

    private void LoadCron()
    {
        if (_stopCancellationTokenSource?.IsCancellationRequested == false)
        {
            _stopCancellationTokenSource.Cancel();
        }

        try
        {
            _crontab = Crontab.Parse(Settings.CronExpression);

            var now = ExactTimeService.GetCurrentLocalDateTime();
            Logger.LogInformation("cron 表达式解析成功，下次触发时间：{}", _crontab.GetNextOccurrence(now).ToLongTimeString());
            _stopCancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(CronWorker, TaskCreationOptions.LongRunning);
        }
        catch (TimeCrontabException ex)
        {
            Logger.LogWarning(ex, "cron 表达式解析失败：");
        }
    }

    public override void UnLoaded()
    {
        Settings.PropertyChanged -= SettingsOnPropertyChanged;
        _stopCancellationTokenSource?.Cancel();
    }
}