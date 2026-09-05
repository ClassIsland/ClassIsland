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
    private readonly HashSet<long> ProcessedNotificationIds = new();
    private readonly HashSet<long> ProcessedIntercomSegmentIds = new();
    private bool IsPolling { get; set; } = false;

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
            Interval = TimeSpan.FromSeconds(5)
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
        return Task.CompletedTask;
    }

    public async Task PollOnceAsync()
    {
        if (IsPolling) return;
        var conn = Connection;
        if (conn == null || string.IsNullOrWhiteSpace(conn.Settings.BashuDeviceToken))
        {
            return;
        }

        IsPolling = true;
        try
        {
            var json = await conn.PollAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // 1. 同步课表（实时更新，精确对齐当天）
            if (root.TryGetProperty("dashboard", out var dashboard))
            {
                if (dashboard.TryGetProperty("schedule", out var scheduleEl) && scheduleEl.ValueKind == JsonValueKind.Array)
                {
                    var sig = scheduleEl.GetRawText();
                    if (sig != LastScheduleSignature)
                    {
                        await ApplyScheduleAsync(scheduleEl);
                        LastScheduleSignature = sig;
                    }
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
                    var author = item.TryGetProperty("created_by_name", out var aEl) ? BashuPlatformConnection.GetStringFlexible(aEl) : "教师";
                    var priority = item.TryGetProperty("priority", out var pEl) ? BashuPlatformConnection.GetStringFlexible(pEl) : "normal";
                    var isEmergency = priority == "emergency";
                    var repeat = item.TryGetProperty("repeat_count", out var rEl) ? Math.Max(1, BashuPlatformConnection.GetInt32Flexible(rEl)) : 1;

                    Logger.LogInformation("收到平台广播通知：[{}] {} (来自 {})", priority, content, author);

                    // 弹出 ClassIsland 原生通知卡片
                    NotificationHostService.ShowNotification(new NotificationRequest
                    {
                        MaskContent = NotificationContent.CreateTwoIconsMask(
                            isEmergency ? $"【紧急广播】来自 {author}" : $"班级通知 · 来自 {author}",
                            rightIcon: "\uE7E7"
                        ),
                        OverlayContent = NotificationContent.CreateRollingTextContent(content, TimeSpan.FromSeconds(6) * repeat, repeat),
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

                    // 直接向语音队列加入朗读文本，确保 Windows / macOS 均能清晰朗读通知内容
                    var speechAnnouncement = isEmergency ? $"紧急广播通知，来自{author}老师：{content}" : $"{author}老师通知：{content}";
                    for (int i = 0; i < repeat; i++)
                    {
                        SpeechService.EnqueueSpeechQueue(speechAnnouncement);
                    }

                    // 向平台确认收到
                    _ = conn.AcknowledgeNotificationAsync(id);
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

                    ProcessedIntercomSegmentIds.Add(segId);
                    var author = segment.TryGetProperty("created_by_name", out var aEl) ? BashuPlatformConnection.GetStringFlexible(aEl) : "教师";
                    var priority = segment.TryGetProperty("priority", out var pEl) ? BashuPlatformConnection.GetStringFlexible(pEl) : "normal";
                    var sequence = segment.TryGetProperty("sequence", out var sEl) ? BashuPlatformConnection.GetInt32Flexible(sEl) : 1;
                    var mimeType = segment.TryGetProperty("mime_type", out var mEl) ? BashuPlatformConnection.GetStringFlexible(mEl) : "audio/webm";
                    var isEmergency = priority == "emergency";

                    Logger.LogInformation("收到平台对讲音频片段 #{} 来自 {}", sequence, author);

                    // 弹出实时对讲专用卡片
                    NotificationHostService.ShowNotification(new NotificationRequest
                    {
                        MaskContent = NotificationContent.CreateTwoIconsMask(
                            isEmergency ? $"【紧急对讲】{author} 正在讲话" : $"【实时对讲】{author} 正在讲话",
                            rightIcon: "\uE720"
                        ),
                        OverlayContent = NotificationContent.CreateSimpleTextContent($"{author} 正在讲话 (片段 #{sequence})"),
                        IsPriorityOverride = isEmergency,
                        PriorityOverride = isEmergency ? 110 : 80,
                        RequestNotificationSettings =
                        {
                            IsSettingsEnabled = true,
                            IsSpeechEnabled = false,
                            IsNotificationSoundEnabled = false,
                            IsNotificationTopmostEnabled = true
                        }
                    }, Guid.Empty, Guid.Empty, true, false);

                    // 异步下载并播放音频片段
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var audioBytes = await conn.GetIntercomSegmentAudioAsync(segId);
                            if (audioBytes != null && audioBytes.Length > 0)
                            {
                                await PlayIntercomAudioAsync(audioBytes, mimeType, author);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "播放对讲音频片段异常：#{}", segId);
                        }
                        finally
                        {
                            _ = conn.AcknowledgeIntercomSegmentAsync(segId);
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug("执行平台轮询发生错误：{}", ex.Message);
        }
        finally
        {
            IsPolling = false;
        }
    }

    private async Task ApplyScheduleAsync(JsonElement scheduleEl)
    {
        try
        {
            Logger.LogInformation("开始同步两江巴蜀平台课表到 ClassIsland 档案...");

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var profile = ProfileService.Profile;
                var currentTime = ExactTimeService.GetCurrentLocalDateTime();
                var today = currentTime.Date;
                var currentDayOfWeek = (int)currentTime.DayOfWeek;

                // 查找或创建专用的“两江巴蜀云端时间表”
                var timeLayoutId = Guid.Parse("bb001122-3344-5566-7788-99aabbccdde0");
                if (!profile.TimeLayouts.TryGetValue(timeLayoutId, out var timeLayout))
                {
                    timeLayout = new TimeLayout { Name = "两江巴蜀同步作息时间表", IsActivated = true };
                    profile.TimeLayouts[timeLayoutId] = timeLayout;
                }

                // 查找或创建专用的“两江巴蜀云端课表”
                var classPlanId = Guid.Parse("bb001122-3344-5566-7788-99aabbccdde1");
                if (!profile.ClassPlans.TryGetValue(classPlanId, out var classPlan))
                {
                    classPlan = new ClassPlan
                    {
                        Name = "两江巴蜀今日课程表",
                        TimeLayoutId = timeLayoutId,
                        IsEnabled = true,
                        IsActivated = true
                    };
                    profile.ClassPlans[classPlanId] = classPlan;
                }

                // 关键修复 1：明确设置当前星期的触发规则，避免默认 WeekDay = 0 (周日) 导致工作日匹配不到而被当作“明天的课表”
                classPlan.TimeRule.WeekDay = currentDayOfWeek;
                classPlan.TimeRule.WeekCountDiv = 0;
                classPlan.TimeRule.WeekCountDivTotal = 2;
                classPlan.AssociatedGroup = ClassPlanGroup.GlobalGroupGuid;
                classPlan.IsEnabled = true;
                classPlan.IsActivated = true;
                timeLayout.IsActivated = true;

                // 关键修复 2：设置为今日临时课表，优先级最高，确保无论何时查询当天都直接返回此表
                profile.TempClassPlanId = classPlanId;
                profile.TempClassPlanSetupTime = today;

                timeLayout.Layouts.Clear();
                classPlan.Classes.Clear();

                int lessonIndex = 0;
                TimeSpan? previousEndTime = null;

                foreach (var lessonEl in scheduleEl.EnumerateArray())
                {
                    var subjectName = lessonEl.TryGetProperty("subject_name", out var sName) ? sName.GetString() ?? "" : "";
                    var lessonTitle = lessonEl.TryGetProperty("lesson_title", out var lTitle) ? lTitle.GetString() ?? "" : "";
                    var teacherName = lessonEl.TryGetProperty("teacher_name", out var tName) ? tName.GetString() ?? "" : "";
                    var classroom = lessonEl.TryGetProperty("classroom", out var cRoom) ? cRoom.GetString() ?? "" : "";
                    var startsAtStr = lessonEl.TryGetProperty("starts_at", out var sAt) ? sAt.GetString() ?? "" : "";
                    var endsAtStr = lessonEl.TryGetProperty("ends_at", out var eAt) ? eAt.GetString() ?? "" : "";

                    var displayName = !string.IsNullOrWhiteSpace(subjectName) ? subjectName : (!string.IsNullOrWhiteSpace(lessonTitle) ? lessonTitle : "自习");
                    if (displayName == "自习") displayName = "自主学习";

                    if (!TimeSpan.TryParse(startsAtStr, out var startTime) || !TimeSpan.TryParse(endsAtStr, out var endTime))
                    {
                        continue;
                    }

                    // 若与上一节课之间存在空隙，自动插入“课间休息”
                    if (previousEndTime.HasValue && startTime > previousEndTime.Value)
                    {
                        timeLayout.Layouts.Add(new TimeLayoutItem
                        {
                            StartTime = previousEndTime.Value,
                            EndTime = startTime,
                            TimeType = 1, // 课间休息
                            BreakName = "课间休息"
                        });
                    }

                    // 添加上课节次到时间表
                    timeLayout.Layouts.Add(new TimeLayoutItem
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        TimeType = 0 // 上课
                    });

                    // 确保科目在 Profile.Subjects 中存在
                    var subject = profile.Subjects.Values.FirstOrDefault(s => s.Name == displayName && s.TeacherName == teacherName);
                    if (subject == null)
                    {
                        var subjectId = Guid.NewGuid();
                        var initial = displayName.Length > 0 ? displayName[..1] : "课";
                        subject = new Subject
                        {
                            Name = displayName,
                            Initial = initial,
                            TeacherName = teacherName
                        };
                        profile.Subjects[subjectId] = subject;
                    }
                    var existingSubjectId = profile.Subjects.First(kv => kv.Value == subject).Key;

                    // 关联至课表
                    classPlan.Classes.Add(new ClassInfo
                    {
                        Index = lessonIndex++,
                        SubjectId = existingSubjectId,
                        CurrentTimeLayout = timeLayout
                    });

                    previousEndTime = endTime;
                }

                // 关键修复 3：通知 Profile 与 LessonsService 立即刷新，实现即时更新无需重启
                profile.RefreshTimeLayouts();
                ProfileService.SaveProfile(ProfileService.CurrentProfilePath);

                LessonsService.CurrentClassPlan = classPlan;
                LessonsService.StartMainTimer();

                Logger.LogInformation("两江巴蜀课表同步完成，共加载 {} 节课程与对应时间点，已实时刷新主界面。", lessonIndex);
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "转换并应用平台课表发生异常");
        }
    }

    private async Task PlayIntercomAudioAsync(byte[] audioBytes, string mimeType, string author)
    {
        var ext = mimeType.Contains("mp4") ? ".mp4" :
                  mimeType.Contains("ogg") ? ".ogg" :
                  mimeType.Contains("wav") ? ".wav" :
                  mimeType.Contains("mpeg") || mimeType.Contains("mp3") ? ".mp3" : ".webm";
        var tempFile = Path.Combine(Path.GetTempPath(), $"intercom_{Guid.NewGuid()}{ext}");
        try
        {
            await File.WriteAllBytesAsync(tempFile, audioBytes);

            // 1. macOS 系统播放
            if (OperatingSystem.IsMacOS())
            {
                if (File.Exists("/opt/homebrew/bin/ffplay") || File.Exists("/usr/local/bin/ffplay"))
                {
                    var ffplayPath = File.Exists("/opt/homebrew/bin/ffplay") ? "/opt/homebrew/bin/ffplay" : "/usr/local/bin/ffplay";
                    using var proc = Process.Start(new ProcessStartInfo
                    {
                        FileName = ffplayPath,
                        Arguments = $"-nodisp -autoexit -loglevel quiet \"{tempFile}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    if (proc != null) await proc.WaitForExitAsync();
                    return;
                }

                if (!ext.Contains("webm"))
                {
                    using var proc = Process.Start(new ProcessStartInfo
                    {
                        FileName = "/usr/bin/afplay",
                        Arguments = $"\"{tempFile}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    if (proc != null) await proc.WaitForExitAsync();
                    return;
                }
            }

            // 2. ClassIsland 原生 MiniAudio 音频播放
            try
            {
                await AudioService.PlayAudioAsync(tempFile, (float)SettingsService.Settings.SpeechVolume);
                return;
            }
            catch
            {
                // 音频格式暂不被 MiniAudio 支持时，继续尝试系统播放器
            }

            // 3. Windows 系统播放
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-NoProfile -Command \"Add-Type -AssemblyName presentationCore; $p = New-Object System.Windows.Media.MediaPlayer; $p.Open('{tempFile.Replace("'", "''")}'); $p.Play(); Start-Sleep -Milliseconds 1200\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc != null) await proc.WaitForExitAsync();
                    return;
                }
                catch (Exception ex)
                {
                    Logger.LogDebug("PowerShell 播放音频发生异常：{}", ex.Message);
                }
            }

            // 4. 若无法解码特殊格式，通过 TTS 播报兜底
            SpeechService.EnqueueSpeechQueue($"{author}老师发来一段语音对讲");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "播放对讲音频失败");
        }
        finally
        {
            try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
        }
    }
}
