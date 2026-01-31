using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Enums.Notification;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Notification;
using Microsoft.Extensions.Logging;
/*
 * このまま未完成なままでいいの
 * 就这样有始无终便可以
 * エンドロールはいらないから
 * 因为我们并不需要片尾的演职员表
 * 壊れた時計は魔法をかけてももう動かないわ
 * 支离破碎的时针即便施加魔法也难以使其再次运转
 * 君との甘すぎる夢の中で泳いでいる事も忘れて
 * 遨游在与你如痴如醉的梦境之中使我流连忘返
 * 誰も知らないこの物語は二人だけのランデヴー
 * 无人知晓的这篇物语便是独属于二人的桑间之约
 *             —— 未完成ランデヴー - Lezel
 */
using NotificationRequest = ClassIsland.Core.Models.Notification.NotificationRequest;


namespace ClassIsland.Services;

public class NotificationWorkerService : INotificationWorkerService
{
    
    private ISpeechService SpeechService { get; }
    private IAudioService AudioService { get; }
    private SettingsService SettingsService { get; }
    private IExactTimeService ExactTimeService { get; }
    private ILessonsService LessonsService { get; }
    private ILogger<NotificationWorkerService> Logger { get; }

    private List<(NotificationRequest request, bool overlay)> PlayingRequests { get; } = [];
    
    public NotificationWorkerService(ISpeechService speechService,
        IAudioService audioService, 
        SettingsService settingsService,
        IExactTimeService exactTimeService,
        ILessonsService lessonsService,
        ILogger<NotificationWorkerService> logger)
    {
        SpeechService = speechService;
        AudioService = audioService;
        SettingsService = settingsService;
        ExactTimeService = exactTimeService;
        LessonsService = lessonsService;
        Logger = logger;
        
        LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
    }

    private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        var now = ExactTimeService.GetCurrentLocalDateTime();
        foreach (var (request, overlay) in PlayingRequests)
        {
            if (!overlay || request.OverlayContent is not {} content)
            {
                continue;
            }

            var session = request.OverlaySession;
            request.LeftProgress = session.IsExplicitEndTime
                ? 1 - (now - session.SessionStartTime) / content.Duration
                : 1 - (session.SessionPlayedTime + session.TimingStopwatch.Elapsed) / content.Duration;
        }
    }
    
    private TimeSpan SetupNotificationSessionTiming(NotificationContent content, NotificationPlayingSessionInfo session)
    {
        var now = ExactTimeService.GetCurrentLocalDateTime();
        var explicitEndTime = content.EndTime != null;
        if (!content.IsTimingInit && explicitEndTime)  // 如果目标结束时间不为空，那么就计算持续时间
        {
            var rawTime = content.EndTime!.Value - now;
            content.Duration = rawTime > TimeSpan.Zero ? rawTime : TimeSpan.Zero;
        }

        if (session.SessionStartTime == DateTime.MinValue)
        {
            session.SessionStartTime = now;
        }

        session.IsExplicitEndTime = explicitEndTime;
        session.CurrentTicketStartTime = now;
        session.TimingStopwatch.Restart();
        content.IsTimingInit = true;

        var duration = explicitEndTime
            ? content.EndTime!.Value - now
            : content.Duration - session.SessionPlayedTime;
        return duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
    }

    public NotificationPlayingTicket CreateTicket(NotificationRequest request)
    {
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(request.CancellationToken);
        INotificationSettings settings = SettingsService.Settings;
        foreach (var i in new List<NotificationSettings?>([
                         request.ChannelSettings,
                         request.ProviderSettings,
                         request.RequestNotificationSettings
                     ]).OfType<NotificationSettings>()
                     .Where(i => i.IsSettingsEnabled))
        {
            settings = i;
            break;
        }

        var ticket = new NotificationPlayingTicket()
        {
            ProcessMask = CreateMaskProcessor(request, cancellationTokenSource.Token, settings),
            ProcessOverlay = CreateOverlayProcessor(request, cancellationTokenSource.Token, settings),
            Request = request,
            Settings = settings,
            CancellationTokenSource = cancellationTokenSource
        };
        return ticket;
    }

    private Func<Task> CreateMaskProcessor(NotificationRequest request, CancellationToken cancellationToken, INotificationSettings settings) => async () =>
    {
        await ProcessNotificationSessionCore(request, request.MaskContent, request.MaskSession, true, cancellationToken, settings);
    };
    
    private Func<Task> CreateOverlayProcessor(NotificationRequest request, CancellationToken cancellationToken, INotificationSettings settings) => async () =>
    {
        if (request.OverlayContent == null)
        {
            return;
        }
        await ProcessNotificationSessionCore(request, request.OverlayContent, request.OverlaySession, false, cancellationToken, settings);
    };

    private async Task ProcessNotificationSessionCore(NotificationRequest request,
        NotificationContent content,
        NotificationPlayingSessionInfo session,
        bool isMask,
        CancellationToken cancellationToken, 
        INotificationSettings settings)
    {
        var duration = SetupNotificationSessionTiming(content, session);
        request.State = NotificationState.Playing;
        var tuple = (request, !isMask);
        PlayingRequests.Add(tuple);
        using var soundsCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            var isSpeechEnabled = settings.IsSpeechEnabled && content.IsSpeechEnabled && SettingsService.Settings.AllowNotificationSpeech;
            if (isSpeechEnabled)
            {
                SpeechService.EnqueueSpeechQueue(content.SpeechContent);
            }
            
            if (isMask && settings.IsNotificationSoundEnabled && SettingsService.Settings.AllowNotificationSound)
            {
                try
                {
                    Logger.LogInformation("即将播放提醒音效：{}", settings.NotificationSoundPath);
                    _ = AudioService.PlayAudioAsync(string.IsNullOrWhiteSpace(settings.NotificationSoundPath)
                            ? AssetLoader.Open(INotificationProvider.DefaultNotificationSoundUri)
                            : File.OpenRead(settings.NotificationSoundPath),
                        (float)SettingsService.Settings.NotificationSoundVolume, soundsCts.Token);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "无法播放提醒音效：{}", settings.NotificationSoundPath);
                }
            }
            
            session.HasSoundsPlayed = true;
            await Task.Delay(duration, cancellationToken);
            if (request.OverlayContent == null || !isMask)
            {
                request.State = NotificationState.Completed;
            }
        }
        catch (TaskCanceledException)
        {
            Logger.LogInformation("提醒请求 {request} 取消遮罩播放", request.GetHashCode());
            request.State = request.CancellationToken.IsCancellationRequested
                ? NotificationState.Cancelled
                : NotificationState.Paused;
            throw;
        }
        catch
        {
            request.State = NotificationState.Paused;
            throw;
        }
        finally
        {
            await soundsCts.CancelAsync();
            PlayingRequests.Remove(tuple);
        }
    }
    
}