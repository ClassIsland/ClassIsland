using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared.Abstraction.Services;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.Management;

/// <summary>
/// 两江巴蜀智慧教研平台后台同步托管服务
/// 负责定时拉取并自动转换课表到 ClassIsland Profile、
/// 弹窗播放班级通知（带 1.wav 提示音与 TTS 朗读）、
/// 并向平台反馈确认回执
/// </summary>
public class BashuPlatformService : IHostedService
{
    private ILogger<BashuPlatformService> Logger { get; }
    private IProfileService ProfileService { get; }
    private INotificationHostService NotificationHostService { get; }
    private IManagementService ManagementService { get; }

    private DispatcherTimer? PollTimer { get; set; }
    private string LastScheduleSignature { get; set; } = "";
    private readonly HashSet<long> ProcessedNotificationIds = new();
    private bool IsPolling { get; set; } = false;

    public BashuPlatformConnection? Connection => ManagementService.Connection as BashuPlatformConnection;

    public BashuPlatformService(
        ILogger<BashuPlatformService> logger,
        IProfileService profileService,
        INotificationHostService notificationHostService,
        IManagementService managementService)
    {
        Logger = logger;
        ProfileService = profileService;
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

            // 1. 同步课表
            if (root.TryGetProperty("dashboard", out var dashboard))
            {
                if (dashboard.TryGetProperty("schedule", out var scheduleEl) && scheduleEl.ValueKind == JsonValueKind.Array)
                {
                    var sig = scheduleEl.GetRawText();
                    if (sig != LastScheduleSignature)
                    {
                        ApplySchedule(scheduleEl);
                        LastScheduleSignature = sig;
                    }
                }
            }

            // 2. 接收与播报通知
            if (root.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in itemsEl.EnumerateArray())
                {
                    var id = item.TryGetProperty("id", out var idEl) ? idEl.GetInt64() : 0;
                    if (id <= 0 || ProcessedNotificationIds.Contains(id))
                    {
                        continue;
                    }

                    ProcessedNotificationIds.Add(id);
                    var content = item.TryGetProperty("content", out var cEl) ? cEl.GetString() ?? "" : "";
                    var author = item.TryGetProperty("created_by_name", out var aEl) ? aEl.GetString() ?? "教师" : "教师";
                    var priority = item.TryGetProperty("priority", out var pEl) ? pEl.GetString() ?? "normal" : "normal";
                    var isEmergency = priority == "emergency";
                    var repeat = item.TryGetProperty("repeat_count", out var rEl) ? Math.Max(1, rEl.GetInt32()) : 1;

                    Logger.LogInformation("收到平台通知：[{}] {} (来自 {})", priority, content, author);

                    // 弹出 ClassIsland 原生通知卡片（含 1.wav 提示音与 TTS 朗读）
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

                    // 向平台确认收到
                    _ = conn.AcknowledgeNotificationAsync(id);
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

    private void ApplySchedule(JsonElement scheduleEl)
    {
        try
        {
            Logger.LogInformation("开始同步两江巴蜀平台课表到 ClassIsland 档案...");
            var profile = ProfileService.Profile;

            // 查找或创建专用的“两江巴蜀云端时间表”
            var timeLayoutId = Guid.Parse("bb001122-3344-5566-7788-99aabbccdde0");
            if (!profile.TimeLayouts.TryGetValue(timeLayoutId, out var timeLayout))
            {
                timeLayout = new TimeLayout { Name = "两江巴蜀同步作息时间表" };
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
                    IsEnabled = true
                };
                profile.ClassPlans[classPlanId] = classPlan;
            }

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

            profile.RefreshTimeLayouts();
            ProfileService.SaveProfile(ProfileService.CurrentProfilePath);
            Logger.LogInformation("两江巴蜀课表同步完成，共加载 {} 节课程与对应时间点。", lessonIndex);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "转换平台课表发生异常");
        }
    }
}
