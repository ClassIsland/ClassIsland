using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Enums.Notification;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared;
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

    private readonly object _playingRequestsLock = new();
    private List<(NotificationRequest request, bool overlay)> PlayingRequests { get; } = [];
    private readonly HashSet<CancellationTokenSource> _activeAudioTokens = new();

    /// <summary>
    /// 取消所有正在播放的音效令牌。
    /// </summary>
    public void CancelAllAudio()
    {
        CancellationTokenSource[] tokens;
        lock (_playingRequestsLock)
        {
            tokens = [.. _activeAudioTokens];
            _activeAudioTokens.Clear();
        }
        foreach (var token in tokens)
        {
            token.Cancel();
        }
        SpeechService.ClearSpeechQueue();
    }
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

    private void TransitionState(NotificationRequest request, NotificationState newState)
    {
        IAppHost.GetService<INotificationHostService>().TransitionRequestState(request, newState);
    }

    private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        var now = ExactTimeService.GetCurrentLocalDateTime();
        List<(NotificationRequest request, bool overlay)> requests;
        lock (_playingRequestsLock)
        {
            requests = [.. PlayingRequests];
        }

        foreach (var (request, overlay) in requests)
        {
            if (!overlay || request.OverlayContent is not { } content)
            {
                continue;
            }

            var session = request.OverlaySession;
            if (request.State == NotificationState.Paused)
                continue;
            
            request.LeftProgress = session.IsExplicitEndTime
                ? 1 - (now - session.SessionStartTime) / content.Duration
                : 1 - (session.SessionPlayedTime + session.TimingStopwatch.Elapsed) / content.Duration;
        }
    }
    
    private TimeSpan SetupNotificationSessionTiming(Guid sid, NotificationContent content, NotificationPlayingSessionInfo session)
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
        Logger.LogTrace("[{sid}] 计算当前票据会话持续时间，now={now}, playedTime={playedTime}, duration={duration}", sid, now, session.SessionPlayedTime, duration);
        return duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
    }

    public NotificationPlayingTicket CreateTicket(NotificationRequest request)
    {
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            request.CancellationToken);
        var settings = GetEffectiveSettings(request);

        var cancellationCompletedSource = new TaskCompletionSource();
        cancellationTokenSource.Token.Register(() =>
        {
            if (request.State != NotificationState.Playing)
            {
                cancellationCompletedSource.TrySetResult();
            }
        });
        var ticket = new NotificationPlayingTicket()
        {
            ProcessMask = CreateMaskProcessor(request, cancellationTokenSource.Token, settings, cancellationCompletedSource),
            ProcessOverlay = CreateOverlayProcessor(request, cancellationTokenSource.Token, settings, cancellationCompletedSource),
            Request = request,
            Settings = settings,
            CancellationTokenSource = cancellationTokenSource,
            CancellationCompletedCompletionSource = cancellationCompletedSource
        };
        return ticket;
    }

    private INotificationSettings GetEffectiveSettings(NotificationRequest request)
    {
        INotificationSettings settings = SettingsService.Settings;
        var candidates = new[]
        {
            request.ChannelSettings,
            request.ProviderSettings,
            request.RequestNotificationSettings
        };

        foreach (var candidate in candidates.OfType<NotificationSettings>().Where(i => i.IsSettingsEnabled))
        {
            settings = candidate;
            break;
        }

        return settings;
    }

    private Func<Task> CreateMaskProcessor(NotificationRequest request, CancellationToken cancellationToken, INotificationSettings settings, TaskCompletionSource cancellationCompletedSource) => async () =>
    {
        await ProcessNotificationSessionCore(request, request.MaskContent, request.MaskSession, true, cancellationToken, settings, cancellationCompletedSource);
    };
    
    private Func<Task> CreateOverlayProcessor(NotificationRequest request, CancellationToken cancellationToken, INotificationSettings settings, TaskCompletionSource cancellationCompletedSource) => async () =>
    {
        if (request.OverlayContent == null)
        {
            return;
        }
        await ProcessNotificationSessionCore(request, request.OverlayContent, request.OverlaySession, false, cancellationToken, settings, cancellationCompletedSource);
    };

    private async Task ProcessNotificationSessionCore(NotificationRequest request,
        NotificationContent content,
        NotificationPlayingSessionInfo session,
        bool isMask,
        CancellationToken cancellationToken, 
        INotificationSettings settings,
        TaskCompletionSource cancellationCompletedSource)
    {
        var id = Guid.NewGuid();
        var duration = SetupNotificationSessionTiming(id, content, session);
        TransitionState(request, NotificationState.Playing);
        var tuple = (request, !isMask);
        lock (_playingRequestsLock)
        {
            PlayingRequests.Add(tuple);
        }
        // 音效令牌独立于请求的取消令牌，移交时不会被取消。
        CancellationTokenSource? audioCancellationTokenSource = null;
        Logger.LogTrace("[{id}] Start session, isMask={isMask}, duration={duration}", id, isMask, duration);
        try
        {
            var isSpeechEnabled = settings.IsSpeechEnabled && content.IsSpeechEnabled && SettingsService.Settings.AllowNotificationSpeech;
            if (!session.HasSoundsPlayed && isSpeechEnabled)
            {
                try { SpeechService.EnqueueSpeechQueue(content.SpeechContent); } catch (Exception ex) { Logger.LogWarning(ex, "语音播报失败"); }
            }
            
            if (!session.HasSoundsPlayed && isMask && settings.IsNotificationSoundEnabled && SettingsService.Settings.AllowNotificationSound)
            {
                try
                {
                    Logger.LogInformation("即将播放提醒音效：{}", settings.NotificationSoundPath);
                    // 音效令牌独立于请求的取消令牌，移交时不会被取消。
                    audioCancellationTokenSource = new CancellationTokenSource();
                    lock (_playingRequestsLock)
                    {
                        _activeAudioTokens.Add(audioCancellationTokenSource);
                    }
                    _ = PlayNotificationSoundAsync(settings, audioCancellationTokenSource);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "无法播放提醒音效：{}", settings.NotificationSoundPath);
                }
            }
            
            session.HasSoundsPlayed = true;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var now = ExactTimeService.GetCurrentLocalDateTime();
                if (request.State == NotificationState.Paused)
                {
                    if (session.TimingStopwatch.IsRunning)
                    {
                        session.TimingStopwatch.Stop();
                        session.SessionPlayedTime += session.TimingStopwatch.Elapsed;
                        session.TimingStopwatch.Reset();
                    }

                    await Task.Delay(100, cancellationToken);
                    continue;
                }

                if (!session.TimingStopwatch.IsRunning)
                {
                    session.TimingStopwatch.Start();
                }

                var remaining = session.IsExplicitEndTime
                    ? content.EndTime!.Value - now
                    : content.Duration - session.SessionPlayedTime - session.TimingStopwatch.Elapsed;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                await Task.Delay((int)Math.Min(100, remaining.TotalMilliseconds), cancellationToken);
            }
            if (request.OverlayContent == null || !isMask)
            {
                TransitionState(request, NotificationState.Completed);
                if (!settings.AllowSpeechContinueAfterEnd)
                {
                    try { SpeechService.ClearSpeechQueue(); } catch (ObjectDisposedException) { }
                }
                await request.CompletedTokenSource.CancelAsync();
            }

            session.IsCompleted = true;
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("提醒请求 {request} 取消播放", request.GetHashCode());
            if (request.CancellationToken.IsCancellationRequested)
            {
                TransitionState(request, NotificationState.Cancelled);
                request.CompletedTokenSource.Cancel();
            }
            else
            {
                TransitionState(request, NotificationState.Interrupted);
                Logger.LogInformation("提醒请求 {request} 中断", request.GetHashCode());
            }
            throw;
        }
        catch
        {
            TransitionState(request, NotificationState.Paused);
            throw;
        }
        finally
        {
            if (session.TimingStopwatch.IsRunning)
            {
                var playedTime = session.TimingStopwatch.Elapsed;
                session.TimingStopwatch.Reset();
                session.SessionPlayedTime += playedTime;
            }
            // 音频截断逻辑：
            // Interrupted: 不截断
            // Cancelled: 直接截断
            // 其他: 看设置决定
            if (request.State == NotificationState.Interrupted)
            {
                // Interrupted
            }
            else if (request.State == NotificationState.Cancelled)
            {
                // Cancelled
                try { audioCancellationTokenSource?.Cancel(); } catch (ObjectDisposedException) { }
            }
            else
            {
                if (!settings.AllowSoundContinueAfterEnd)
                {
                    try { audioCancellationTokenSource?.Cancel(); } catch (ObjectDisposedException) { }
                }
            }
            if (audioCancellationTokenSource != null)
            {
                lock (_playingRequestsLock)
                {
                    _activeAudioTokens.Remove(audioCancellationTokenSource);
                }
            }
            Logger.LogTrace("[{id}] END session, isMask={isMask}, playedTime={playedTime}", id, isMask, session.SessionPlayedTime);
            lock (_playingRequestsLock)
            {
                PlayingRequests.Remove(tuple);
            }
            cancellationCompletedSource.TrySetResult();
        }
    }

    private async Task PlayNotificationSoundAsync(INotificationSettings settings, CancellationTokenSource cancellationTokenSource)
    {
        // 生命周期管理在 ProcessNotificationSessionCore
        try
        {
            var cancellationToken = cancellationTokenSource.Token;
            if (string.IsNullOrWhiteSpace(settings.NotificationSoundPath))
            {
                using var stream = AssetLoader.Open(INotificationProvider.DefaultNotificationSoundUri);
                await AudioService.PlayAudioAsync(stream,
                    (float)SettingsService.Settings.NotificationSoundVolume, cancellationToken);
            }
            else
            {
                await AudioService.PlayAudioAsync(settings.NotificationSoundPath,
                    (float)SettingsService.Settings.NotificationSoundVolume, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "音效播放失败");
        }
    }
    
}
