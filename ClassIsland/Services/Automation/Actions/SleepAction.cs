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
    protected override async Task OnInvoke()
    {
        await base.OnInvoke();
        ActionItem.Progress = 0;
        var sw = Stopwatch.StartNew();
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var dueTimeChanged = false;
        Timer? timer = null;
        timer = new Timer(_ => CheckAndReport(), null, 0, 1000);
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(20, InterruptCancellationToken);
                CheckAndReport();
            }
            catch (TaskCanceledException) {}
        });


        PropertyChangedEventHandler handler = (s, e) =>
        {
            if (e.PropertyName == nameof(Settings.Value))
                CheckAndReport();
        };
        Settings.PropertyChanged += handler;

        using var reg = InterruptCancellationToken.Register(() => tcs.TrySetCanceled());

        try
        {
            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            await timer.DisposeAsync();
            Settings.PropertyChanged -= handler;
        }
        return;

        void CheckAndReport()
        {
            var targetSeconds = Settings.Value;
            var elapsed = sw.Elapsed.TotalSeconds;
            var remaining = targetSeconds - elapsed;

            if (remaining <= 0)
            {
                ActionItem.Progress = 100;
                tcs.TrySetResult(null);
                return;
            }

            var pct = Math.Clamp(elapsed / targetSeconds * 100, 0, 100);
            ActionItem.Progress = pct;

            if (remaining <= 1)
            {
                dueTimeChanged = true;
                timer?.Change((int)(remaining * 1000), 100);
            }
            else
            {
                if (dueTimeChanged)
                {
                    dueTimeChanged = false;
                    timer?.Change(1, 1000);
                }
            }
        }
    }
}