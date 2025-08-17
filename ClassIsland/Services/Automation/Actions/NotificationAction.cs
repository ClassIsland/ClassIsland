using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Helpers;
using ClassIsland.Models.Actions;
using ClassIsland.Services.NotificationProviders;
using ClassIsland.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace ClassIsland.Services.Automation.Actions;

[ActionInfo("classisland.showNotification", "显示提醒", "\ue02b", addDefaultToMenu:false)]
public class NotificationAction : ActionBase<NotificationActionSettings>
{
    static ActionNotificationProvider ActionNotificationProvider { get; } =
        IAppHost.Host.Services.GetServices<IHostedService>().OfType<ActionNotificationProvider>().First();

    protected override async Task OnInvoke()
    {
        var task = ShowNotificationAsync(Settings);
        if (Settings.IsWaitForCompleteEnabled)
        {
            var cancellationRegistration = InterruptCancellationToken.Register(async () => await OnRevert());
            await task;
            await cancellationRegistration.DisposeAsync();
        }
    }

    async Task OnRevert()
    {
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            _cancellationTokenSource?.Cancel();
            _completedTokenSource?.Cancel();
        });
    }



    CancellationTokenSource? _cancellationTokenSource;
    CancellationTokenSource? _completedTokenSource;

    async Task ShowNotificationAsync(NotificationActionSettings settings)
    {
        var request = new NotificationRequest
        {
            MaskContent = NotificationContent.CreateTwoIconsMask(settings.Mask, hasRightIcon:false, factory: x =>
            {
                x.Duration = TimeSpanHelper.FromSecondsSafe(settings.MaskDurationSeconds);
                x.IsSpeechEnabled = settings.IsMaskSpeechEnabled;
            }),
            OverlayContent = string.IsNullOrEmpty(settings.Content) || settings.ContentDurationSeconds <= 0
                ? null
                : NotificationContent.CreateSimpleTextContent(settings.Content, factory: x =>
                {
                    x.IsSpeechEnabled = settings.IsContentSpeechEnabled;
                    x.Duration = TimeSpanHelper.FromSecondsSafe(settings.ContentDurationSeconds);
                }),
            RequestNotificationSettings =
            {
                IsSettingsEnabled = settings.IsAdvancedSettingsEnabled,
                IsNotificationEffectEnabled = settings.IsEffectEnabled,
                IsNotificationSoundEnabled = settings.IsSoundEffectEnabled,
                IsNotificationTopmostEnabled = settings.IsTopmostEnabled,
                NotificationSoundPath = settings.CustomSoundEffectPath,
                IsSpeechEnabled = true
            }
        };
        _cancellationTokenSource = request.CancellationTokenSource;
        _completedTokenSource = request.CompletedTokenSource;
        await ActionNotificationProvider.ShowNotificationAsync(request);
    }
}