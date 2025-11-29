using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.Actions;
namespace ClassIsland.Services.Automation.Actions;

[ActionInfo("classisland.action.sleep", "等待时长", "\ue9a8")]
public class SleepAction : ActionBase<SleepActionSettings>
{
    readonly Stopwatch _sw = Stopwatch.StartNew();

    protected override async Task OnInvoke()
    {
        await base.OnInvoke();
        Settings.PropertyChanged += OnPropertyChanged;

        try
        {
            while (!InterruptCancellationToken.IsCancellationRequested)
            {
                var targetMs = Settings.Value * 1000;
                var elapsedMs = _sw.ElapsedMilliseconds;

                if (elapsedMs >= targetMs)
                {
                    ActionItem.Progress = 100;
                    break;
                }

                if (ActionItem.Progress == null)
                    ActionItem.Progress = 10 / Settings.Value;
                else
                    ActionItem.Progress = elapsedMs * 100 / targetMs;

                var remainingMs = (int)(targetMs - elapsedMs);
                var checkInterval = Math.Min(remainingMs, 1000);
                await Task.Delay(checkInterval, InterruptCancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            Settings.PropertyChanged -= OnPropertyChanged;
        }
    }

    void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.Value))
        {
            var targetMs = Settings.Value * 1000;
            var elapsed = _sw.ElapsedMilliseconds;

            if (elapsed >= targetMs)
            {
                ActionItem.Progress = 100;
            }
            else
            {
                var progress = elapsed * 100 / targetMs;
                ActionItem.Progress = progress;
            }
        }
    }
}