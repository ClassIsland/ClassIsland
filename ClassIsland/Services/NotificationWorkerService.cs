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

/// <summary>
/// 解析结果(调试)
/// </summary>
/// <param name="Settings">通知设置(解析结果)</param>
/// <param name="Source">来源: Request, Channel, Provider, Global</param>
public record ResolvedSettings(INotificationSettings Settings, string Source);

public class NotificationWorkerService : INotificationWorkerService
{
    
    private ISpeechService SpeechService { get; }
    private IAudioService AudioService { get; }
    private SettingsService SettingsService { get; }
    private IExactTimeService ExactTimeService { get; }
    private ILessonsService LessonsService { get; }
    private INotificationBus Bus { get; }
    private ILogger<NotificationWorkerService> Logger { get; }

    private readonly object _playingRequestsLock = new();
    private List<(NotificationRequest request, NotificationContentSnapshot? snapshot, bool isOverlay)> PlayingRequests { get; } = [];
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
            try { token.Cancel(); } catch (ObjectDisposedException) { }
            try { token.Dispose(); } catch (ObjectDisposedException) { }
        }
        SpeechService.ClearSpeechQueue();
    }
    public NotificationWorkerService(ISpeechService speechService,
        IAudioService audioService, 
        SettingsService settingsService,
        IExactTimeService exactTimeService,
        ILessonsService lessonsService,
        INotificationBus bus,
        ILogger<NotificationWorkerService> logger)
    {
        SpeechService = speechService;
        AudioService = audioService;
        SettingsService = settingsService;
        ExactTimeService = exactTimeService;
        LessonsService = lessonsService;
        Bus = bus;
        Logger = logger;
        
        LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
    }

    private void TransitionState(NotificationRequest request, NotificationState newState)
    {
        var oldState = request.Lifecycle.State;
        Bus.RaiseStateChanged(request, oldState, newState);
    }

    private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        var now = ExactTimeService.GetCurrentLocalDateTime();
        List<(NotificationRequest request, NotificationContentSnapshot? snapshot, bool isOverlay)> requests;
        lock (_playingRequestsLock)
        {
            requests = [.. PlayingRequests];
        }

        foreach (var (request, snapshot, isOverlay) in requests)
        {
            if (!isOverlay || snapshot is not { } snap)
            {
                continue;
            }

            var session = request.OverlaySession;
            if (request.Lifecycle.State == NotificationState.Paused)
                continue;
            
            request.Lifecycle.LeftProgress = session.IsExplicitEndTime
                ? 1 - (now - session.SessionStartTime) / snap.Duration
                : 1 - (session.SessionPlayedTime + session.TimingStopwatch.Elapsed) / snap.Duration;
        }
    }
    
    private TimeSpan SetupNotificationSessionTiming(Guid sid, NotificationContentSnapshot snapshot, NotificationPlayingSessionInfo session)
    {
        var now = ExactTimeService.GetCurrentLocalDateTime();
        var explicitEndTime = snapshot.EndTime != null;

        if (session.SessionStartTime == DateTime.MinValue)
        {
            session.SessionStartTime = now;
        }

        session.IsExplicitEndTime = explicitEndTime;
        session.CurrentTicketStartTime = now;
        session.TimingStopwatch.Restart();

        var duration = explicitEndTime
            ? snapshot.EndTime!.Value - now
            : snapshot.Duration - session.SessionPlayedTime;
        Logger.LogTrace("[{sid}] 计算当前票据会话持续时间，now={now}, playedTime={playedTime}, duration={duration}", sid, now, session.SessionPlayedTime, duration);
        return duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
    }

    public NotificationPlayingTicket CreateTicket(NotificationRequest request)
    {
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            request.Lifecycle.CancellationToken);
        var resolved = GetEffectiveSettings(request);
        var settings = resolved.Settings;
        Logger.LogDebug("票据创建: 来源={source}, Channel={channelId}", resolved.Source, request.ChannelId);
        var now = ExactTimeService.GetCurrentLocalDateTime();
        var maskSnapshot = NotificationContentSnapshot.From(request.MaskContent, now);
        var overlaySnapshot = request.OverlayContent != null
            ? NotificationContentSnapshot.From(request.OverlayContent, now)
            : null;

        var cancellationCompletedSource = new TaskCompletionSource();
        cancellationTokenSource.Token.Register(() =>
        {
            if (request.Lifecycle.State != NotificationState.Playing)
            {
                cancellationCompletedSource.TrySetResult();
            }
        });
        var ticket = new NotificationPlayingTicket()
        {
            ProcessMask = CreateMaskProcessor(request, maskSnapshot, cancellationTokenSource.Token, settings, cancellationCompletedSource),
            ProcessOverlay = CreateOverlayProcessor(request, overlaySnapshot, cancellationTokenSource.Token, settings, cancellationCompletedSource),
            Request = request,
            MaskSnapshot = maskSnapshot,
            OverlaySnapshot = overlaySnapshot,
            Settings = settings,
            CancellationTokenSource = cancellationTokenSource,
            CancellationCompletedCompletionSource = cancellationCompletedSource
        };
        return ticket;
    }

    private ResolvedSettings GetEffectiveSettings(NotificationRequest request)
    {
        // 优先级: 请求级 > 渠道级 > 提供方级 > 全局
        if (request.RequestNotificationSettings is { IsSettingsEnabled: true } req)
        {
            return new ResolvedSettings(req, "Request");
        }
        if (request.ChannelSettings is { IsSettingsEnabled: true } ch)
        {
            return new ResolvedSettings(ch, "Channel");
        }
        if (request.ProviderSettings is { IsSettingsEnabled: true } prov)
        {
            return new ResolvedSettings(prov, "Provider");
        }
        return new ResolvedSettings(SettingsService.Settings, "Global");
    }

    private Func<Task> CreateMaskProcessor(NotificationRequest request, NotificationContentSnapshot snapshot, CancellationToken cancellationToken, INotificationSettings settings, TaskCompletionSource cancellationCompletedSource) => async () =>
    {
        await ProcessNotificationSessionCore(request, snapshot, request.MaskSession, true, cancellationToken, settings, cancellationCompletedSource);
    };
    
    private Func<Task> CreateOverlayProcessor(NotificationRequest request, NotificationContentSnapshot? snapshot, CancellationToken cancellationToken, INotificationSettings settings, TaskCompletionSource cancellationCompletedSource) => async () =>
    {
        if (snapshot == null)
        {
            return;
        }
        await ProcessNotificationSessionCore(request, snapshot, request.OverlaySession, false, cancellationToken, settings, cancellationCompletedSource);
    };

    private async Task ProcessNotificationSessionCore(NotificationRequest request,
        NotificationContentSnapshot snapshot,
        NotificationPlayingSessionInfo session,
        bool isMask,
        CancellationToken cancellationToken, 
        INotificationSettings settings,
        TaskCompletionSource cancellationCompletedSource)
    {
        var id = Guid.NewGuid();
        var duration = SetupNotificationSessionTiming(id, snapshot, session);
        TransitionState(request, NotificationState.Playing);
        var tuple = (request, isMask ? null : snapshot, !isMask);
        lock (_playingRequestsLock)
        {
            PlayingRequests.Add(tuple);
        }
        // 音效令牌独立于请求的取消令牌，移交时不会被取消。
        CancellationTokenSource? audioCancellationTokenSource = null;
        Logger.LogTrace("[{id}] Start session, isMask={isMask}, duration={duration}", id, isMask, duration);
        try
        {
            var isSpeechEnabled = settings.IsSpeechEnabled && snapshot.IsSpeechEnabled && SettingsService.Settings.AllowNotificationSpeech;
            if (!session.HasSoundsPlayed && isSpeechEnabled)
            {
                try { SpeechService.EnqueueSpeechQueue(snapshot.SpeechContent); } catch (Exception ex) { Logger.LogWarning(ex, "语音播报失败"); }
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
                if (request.Lifecycle.State == NotificationState.Paused)
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
                    ? snapshot.EndTime!.Value - now
                    : snapshot.Duration - session.SessionPlayedTime - session.TimingStopwatch.Elapsed;
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
                request.Lifecycle.MarkCompleted();
            }

            session.IsCompleted = true;
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("提醒请求 {request} 取消播放", request.GetHashCode());
            if (request.Lifecycle.CancellationToken.IsCancellationRequested)
            {
                TransitionState(request, NotificationState.Cancelled);
                request.Lifecycle.MarkCompleted();
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
            if (request.Lifecycle.State == NotificationState.Interrupted)
            {
            }
            else if (request.Lifecycle.State == NotificationState.Cancelled)
            {
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
                try { audioCancellationTokenSource.Dispose(); } catch (ObjectDisposedException) { }
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
