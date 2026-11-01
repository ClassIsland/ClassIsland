using System;
using System.IO;
using System.Text.Json;
using ClassIsland.Core;
using ClassIsland.Models.Actions;
using ClassIsland.Models.Overlay;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.Overlay;

/// <summary>
/// 把主 App 中手动预览的提醒写入悬浮主界面宿主进程的收件箱。
/// </summary>
/// <remarks>
/// 目前只有 iOS TrollStore 发行版实现了系统级悬浮主界面；其他平台的
/// <see cref="IMainWindowOverlayService.IsSupported"/> 为 <see langword="false"/>，这里会直接跳过。
/// 悬浮主界面的 HUD 宿主进程会轮询 <see cref="InboxDirectory"/>，把请求显示在悬浮岛上。
/// </remarks>
public static class HudPreviewNotificationForwarder
{
    public const string PreviewInboxFolderName = "Inbox";

    private const string HudDataFolderName = "HudData";
    private static readonly TimeSpan MaximumPendingRequestAge = TimeSpan.FromMinutes(2);
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// 预览提醒收件箱目录。
    /// </summary>
    public static string InboxDirectory =>
        Path.Combine(CommonDirectories.AppSharedDocumentsFolderPath, HudDataFolderName, PreviewInboxFolderName);

    /// <summary>
    /// 尝试把一条提醒预览请求转发给悬浮主界面宿主进程。
    /// 悬浮主界面不支持或未启用时返回 <see langword="false"/>，调用方应回退到本地显示。
    /// </summary>
    public static bool TryForward(NotificationActionSettings settings)
    {
        var overlayService = PlatformServices.MainWindowOverlayService;
        if (!overlayService.IsSupported || !overlayService.IsEnabled)
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(InboxDirectory);
            CleanupStaleRequests();
            var request = new HudPreviewNotification
            {
                Settings = settings
            };
            var path = Path.Combine(InboxDirectory, $"{Guid.NewGuid():N}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(request, SerializerOptions));
            Logger?.LogInformation("已将提醒预览转发到悬浮主界面收件箱：{}", path);
            return true;
        }
        catch (Exception exception)
        {
            Logger?.LogError(exception, "无法把提醒预览转发到悬浮主界面，已回退到本地显示");
            return false;
        }
    }

    private static void CleanupStaleRequests()
    {
        foreach (var path in Directory.EnumerateFiles(InboxDirectory, "*.json"))
        {
            try
            {
                if (DateTime.Now - File.GetLastWriteTime(path) > MaximumPendingRequestAge)
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                // 文件可能已被 HUD 宿主进程取走。
            }
        }
    }

    private static ILogger? Logger =>
        IAppHost.TryGetService<ILoggerFactory>()?.CreateLogger(nameof(HudPreviewNotificationForwarder));
}
