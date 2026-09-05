using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Services.SpeechService;
using ClassIsland.Shared.Abstraction.Services;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.Management;

/// <summary>
/// 两江巴蜀智慧教研平台后台同步托管服务
/// 负责定时拉取并自动转换课表到 ClassIsland Profile（实时刷新、消除“今天变明天”）、
/// 弹窗播报班级广播通知（必播语音与 1.wav 提示音）、
/// 实时接收平台对讲语音并播放音频片段、
/// 并向平台反馈确认回执
/// </summary>
public class BashuPlatformService : IHostedService
{
    private ILogger<BashuPlatformService> Logger { get; }
    private IProfileService ProfileService { get; }
    private ILessonsService LessonsService { get; }
    private IExactTimeService ExactTimeService { get; }
    private ISpeechService SpeechService { get; }
    private IAudioService AudioService { get; }
    private SettingsService SettingsService { get; }
    private INotificationHostService NotificationHostService { get; }
    private IManagementService ManagementService { get; }

    private DispatcherTimer? PollTimer { get; set; }
    private string LastScheduleSignature { get; set; } = "";
    private Profile? LastSyncedProfile;
    private readonly HashSet<long> ProcessedNotificationIds = new();
    private readonly HashSet<long> ProcessedIntercomSegmentIds = new();
    private bool IsPolling { get; set; } = false;
    private readonly HashSet<long> PresentedSessions = new();
    private readonly HashSet<long> PendingNotificationAcks = new();
    private readonly HashSet<long> PendingIntercomAcks = new();
    private readonly CancellationTokenSource Shutdown = new();
    private BashuPlatformConnection? LastConnection;
    public string Status { get; private set; } = "等待连接";
    public string LastSync { get; private set; } = "尚未同步";
    private NotificationRequest? IntercomNotification;
    private DateTime LastAudioAt;
    private Task AudioQueue = Task.CompletedTask;
    private readonly HashSet<long> QueuedSegments = new();

    public BashuPlatformConnection? Connection => ManagementService.Connection as BashuPlatformConnection;

    public BashuPlatformService(
        ILogger<BashuPlatformService> logger,
        IProfileService profileService,
        ILessonsService lessonsService,
        IExactTimeService exactTimeService,
        ISpeechService speechService,
        IAudioService audioService,
        SettingsService settingsService,
        INotificationHostService notificationHostService,
        IManagementService managementService)
    {
        Logger = logger;
        ProfileService = profileService;
        LessonsService = lessonsService;
        ExactTimeService = exactTimeService;
        SpeechService = speechService;
        AudioService = audioService;
        SettingsService = settingsService;
        NotificationHostService = notificationHostService;
        ManagementService = managementService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("启动两江巴蜀智慧教研平台同步托管服务");
        PollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        PollTimer.Tick += async (sender, args) => await PollOnceAsync();
        PollTimer.Start();
        _ = PollOnceAsync();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("停止两江巴蜀智慧教研平台同步托管服务");
        PollTimer?.Stop();
        Shutdown.Cancel();
        IntercomNotification?.Cancel();
        return Task.CompletedTask;
    }

    public async Task PollOnceAsync(bool forceSchedule = false)
    {
        if (IsPolling) { Status = "正在同步，请稍候"; return; }
        if (forceSchedule) LastScheduleSignature = "";
        var conn = Connection;
        if (conn == null || string.IsNullOrWhiteSpace(conn.Settings.BashuDeviceToken))
        {
            return;
        }

        IsPolling = true;
        try
        {
            if (LastConnection != conn)
            {
                LastConnection = conn;
                LastScheduleSignature = "";
                ProcessedNotificationIds.Clear(); ProcessedIntercomSegmentIds.Clear();
                PendingNotificationAcks.Clear(); PendingIntercomAcks.Clear(); PresentedSessions.Clear(); QueuedSegments.Clear();
            }
            foreach (var pending in PendingNotificationAcks.ToArray())
                if (await conn.AcknowledgeNotificationAsync(pending)) PendingNotificationAcks.Remove(pending);
            foreach (var pending in PendingIntercomAcks.ToArray())
                if (await conn.AcknowledgeIntercomSegmentAsync(pending)) PendingIntercomAcks.Remove(pending);
            if (IntercomNotification != null && QueuedSegments.Count == 0 && DateTime.UtcNow - LastAudioAt > TimeSpan.FromSeconds(6))
            {
                IntercomNotification.Cancel(); IntercomNotification = null;
            }
            var json = await conn.PollAsync(Shutdown.Token);
            if (string.IsNullOrWhiteSpace(json))
            {
                Status = conn.LastError;
                return;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            Status = "平台已连接";

            // 1. 同步课表（实时更新，精确对齐当天）
            if (root.TryGetProperty("dashboard", out var dashboard))
            {
                if (dashboard.TryGetProperty("scheduleWeek", out var scheduleEl) && scheduleEl.ValueKind == JsonValueKind.Array)
                {
                    var sig = scheduleEl.GetRawText();
                    if (sig != LastScheduleSignature || !ReferenceEquals(LastSyncedProfile, ProfileService.Profile))
                    {
                        await ApplyScheduleAsync(scheduleEl);
                        LastScheduleSignature = sig;
                        LastSyncedProfile = ProfileService.Profile;
                        LastSync = DateTime.Now.ToString("MM-dd HH:mm:ss");
                    }
                }
                else
                {
                    Status = "服务器尚未提供整周课表，请先更新平台服务";
                }
            }

            // 2. 接收广播通知（必播语音与 1.wav 提示音）
            if (root.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in itemsEl.EnumerateArray())
                {
                    var id = item.TryGetProperty("id", out var idEl) ? BashuPlatformConnection.GetInt64Flexible(idEl) : 0;
                    if (id <= 0 || ProcessedNotificationIds.Contains(id))
                    {
                        continue;
                    }

                    ProcessedNotificationIds.Add(id);
                    var content = item.TryGetProperty("content", out var cEl) ? BashuPlatformConnection.GetStringFlexible(cEl) : "";
                    var author = Author(item);
                    var priority = item.TryGetProperty("priority", out var pEl) ? BashuPlatformConnection.GetStringFlexible(pEl) : "normal";
                    var isEmergency = priority == "emergency";
                    var repeat = item.TryGetProperty("repeat_count", out var rEl) ? Math.Clamp(BashuPlatformConnection.GetInt32Flexible(rEl), 1, 10) : 1;

                    Logger.LogInformation("收到平台广播通知：[{}] {} (来自 {})", priority, content, author);

                    // 弹出 ClassIsland 原生通知卡片
                    NotificationHostService.ShowNotification(new NotificationRequest
                    {
                        MaskContent = NotificationContent.CreateTwoIconsMask(
                            isEmergency ? $"【紧急广播】来自 {author}" : $"班级通知 · 来自 {author}",
                            rightIcon: "\uE7E7", factory: mask => mask.IsSpeechEnabled = false
                        ),
                        OverlayContent = NotificationContent.CreateRollingTextContent($"{author}：{content}", BashuNotificationTiming.Duration(content, author, repeat), repeat,
                            overlay => overlay.SpeechContent = string.Join("。", Enumerable.Repeat($"{author}通知：{content}", repeat))),
                        IsPriorityOverride = isEmergency,
                        PriorityOverride = isEmergency ? 100 : 0,
                        RequestNotificationSettings =
                        {
                            IsSettingsEnabled = true,
                            IsSpeechEnabled = true,
                            IsNotificationSoundEnabled = true,
                            IsNotificationTopmostEnabled = true
                        }
                    }, Guid.Empty, Guid.Empty, true, false);

                    // 向平台确认收到
                    PendingNotificationAcks.Add(id);
                    if (await conn.AcknowledgeNotificationAsync(id)) PendingNotificationAcks.Remove(id);
                }
            }

            // 3. 接收实时对讲音频片段
            if (root.TryGetProperty("intercom", out var intercomEl) && intercomEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var segment in intercomEl.EnumerateArray())
                {
                    var segId = segment.TryGetProperty("id", out var sidEl) ? BashuPlatformConnection.GetInt64Flexible(sidEl) : 0;
                    if (segId <= 0 || ProcessedIntercomSegmentIds.Contains(segId))
                    {
                        continue;
                    }

                    if (!QueuedSegments.Add(segId)) continue;
                    var queuedSegment = segment.Clone();
                    AudioQueue = PlayQueuedSegmentAsync(AudioQueue, conn, queuedSegment, segId);
                }
            }
        }
        catch (Exception ex)
        {
            Status = "同步失败：" + ex.Message;
            Logger.LogWarning(ex, "执行平台轮询发生错误");
        }
        finally
        {
            IsPolling = false;
        }
    }

    private async Task PlayQueuedSegmentAsync(Task previous, BashuPlatformConnection conn, JsonElement segment, long segId)
    {
        await previous;
        try
        {
            if (Shutdown.IsCancellationRequested || Connection != conn) return;
                    var author = Author(segment);
                    var sessionId = segment.TryGetProperty("session_id", out var session) ? BashuPlatformConnection.GetInt64Flexible(session) : segId;
                    var mime = segment.TryGetProperty("mime_type", out var mimeEl) ? mimeEl.GetString() ?? "" : "";
                    try
                    {
                        var bytes = await conn.GetIntercomSegmentAudioAsync(segId, Shutdown.Token);
                        if (bytes == null || bytes.Length == 0) return;
                        if (PresentedSessions.Add(sessionId))
                        {
                            IntercomNotification?.Cancel();
                            IntercomNotification = new NotificationRequest
                            {
                                MaskContent = NotificationContent.CreateTwoIconsMask($"实时对讲 · {author}", rightIcon: "lucide(\ue17c)"),
                                OverlayContent = NotificationContent.CreateSimpleTextContent($"{author} 正在讲话", overlay => overlay.Duration = TimeSpan.FromMinutes(20)),
                                IsPriorityOverride = true,
                                PriorityOverride = 110,
                                RequestNotificationSettings = { IsSettingsEnabled = true, IsSpeechEnabled = false, IsNotificationSoundEnabled = false, IsNotificationTopmostEnabled = true }
                            };
                            NotificationHostService.ShowNotification(IntercomNotification, Guid.Empty, Guid.Empty, true, false);
                        }
                        // Keep playback ordered. Only acknowledge audio that actually completed.
                        await PlayIntercomAudioAsync(bytes, mime, author);
                        LastAudioAt = DateTime.UtcNow;
                        ProcessedIntercomSegmentIds.Add(segId);
                        PendingIntercomAcks.Add(segId);
                        if (await conn.AcknowledgeIntercomSegmentAsync(segId)) PendingIntercomAcks.Remove(segId);
                    }
                    catch (Exception ex)
                    {
                        Status = "对讲播放失败，请检查音量、音频设备及网页是否已更新";
                        Logger.LogWarning(ex, "对讲片段 {SegmentId} 未播放成功，不发送成功回执", segId);

                    }
        }
        finally { QueuedSegments.Remove(segId); }
    }

    private static string Author(JsonElement item)
    {
        var name = item.TryGetProperty("created_by_name", out var value) ? value.GetString() : null;
        return string.IsNullOrWhiteSpace(name) ? "平台教师" : name.Trim();
    }

    private async Task ApplyScheduleAsync(JsonElement scheduleEl)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var profile = ProfileService.Profile;
            BashuScheduleMapper.Apply(profile, scheduleEl);
            profile.RefreshTimeLayouts();
            ProfileService.SaveProfile(ProfileService.CurrentProfilePath);
            if (LessonsService is ClassIsland.Services.LessonsService lessons)
                lessons.RefreshAfterPlatformSync();
            LessonsService.StartMainTimer();
        });
    }

    private async Task PlayIntercomAudioAsync(byte[] bytes, string mimeType, string author)
    {
        // The platform records independent PCM WAV segments, avoiding browser-only Opus/WebM decoders.
        if (!mimeType.StartsWith("audio/wav", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("请刷新平台网页后重新发起对讲（需要 PCM WAV 音频）");
        using var lease = await AudioService.TryInitializeDefaultPlaybackDeviceSafeAsync();
        if (lease == null) throw new InvalidOperationException("没有可用的音频输出设备");
        using var audio = new MemoryStream(bytes, false);
        await AudioService.PlayAudioAsync(audio, (float)SettingsService.Settings.SpeechVolume, Shutdown.Token);
        Shutdown.Token.ThrowIfCancellationRequested();
    }
}
