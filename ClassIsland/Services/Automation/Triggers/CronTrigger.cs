using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.Automation.Triggers;
using MaterialDesignThemes.Wpf;
using TimeCrontab;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.cron", "cron", PackIconKind.Repeat)]
public class CronTrigger : TriggerBase<CronTriggerSettings>
{
    private Crontab? _crontab;

    private CancellationTokenSource? _stopCancellationTokenSource;

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

            await Task.Delay(_crontab.GetSleepTimeSpan(DateTime.Now), _stopCancellationTokenSource.Token);
            if (_stopCancellationTokenSource.IsCancellationRequested)
            {
                break;
            }

            AppBase.Current.Dispatcher.Invoke(Trigger);
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
        _crontab = Crontab.TryParse(Settings.CronExpression);
        if (_crontab != null)
        {
            _stopCancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(CronWorker, TaskCreationOptions.LongRunning);
        }
    }

    public override void UnLoaded()
    {
        Settings.PropertyChanged -= SettingsOnPropertyChanged;
        _stopCancellationTokenSource?.Cancel();
    }
}