using System.Text.Json;
using Avalonia.Threading;
using ClassIsland.Core.Models.Notification;
using ClassIsland.iOS.Services.Platform;
using ClassIsland.Models.Actions;
using ClassIsland.Models.Overlay;
using ClassIsland.Services.Automation.Actions;
using ClassIsland.Services.NotificationProviders;
using ClassIsland.Services.Overlay;
using ClassIsland.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.iOS.Services.Overlay;

/// <summary>
/// HUD 宿主进程侧的预览提醒收件箱：轮询主 App 写入的预览请求，并在悬浮岛上显示。
/// </summary>
/// <remarks>
/// 仅在 <c>--hud</c> 宿主进程中使用。只处理"手动预览触发"转发过来的请求，
/// 自动触发的提醒仍由宿主进程自己的自动化服务产生，因此不会出现重复显示。
/// </remarks>
internal sealed class IosHudPreviewNotificationPoller : IDisposable
{
    private const string ShowNotificationKind = "showNotification";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaximumRequestAge = TimeSpan.FromMinutes(2);
    private static readonly JsonSerializerOptions SerializerOptions = new();

    private readonly DispatcherTimer _timer = new()
    {
        Interval = PollInterval
    };
    private bool _isProcessing;
    private bool _isDisposed;

    public IosHudPreviewNotificationPoller()
    {
        _timer.Tick += (_, _) => ProcessInbox();
    }

    public void Start()
    {
        if (_isDisposed)
        {
            return;
        }

        _timer.Start();
        IosStartupDiagnostics.Write($"HUD preview inbox: {GetInboxDirectory()}");
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _timer.Stop();
    }

    private static string GetInboxDirectory()
    {
        var hudDataDirectory = Environment.GetEnvironmentVariable("CLASSISLAND_HUD_DATA_DIR");
        if (string.IsNullOrWhiteSpace(hudDataDirectory))
        {
            return string.Empty;
        }

        // CLASSISLAND_HUD_DATA_DIR 指向 .../HudData/Snapshot-xxx，收件箱与快照目录同级。
        var hudRoot = Path.GetDirectoryName(Path.GetFullPath(hudDataDirectory));
        return string.IsNullOrEmpty(hudRoot)
            ? string.Empty
            : Path.Combine(hudRoot, HudPreviewNotificationForwarder.PreviewInboxFolderName);
    }

    private void ProcessInbox()
    {
        if (_isProcessing || _isDisposed)
        {
            return;
        }

        _isProcessing = true;
        try
        {
            var inboxDirectory = GetInboxDirectory();
            if (string.IsNullOrEmpty(inboxDirectory) || !Directory.Exists(inboxDirectory))
            {
                return;
            }

            foreach (var path in Directory.EnumerateFiles(inboxDirectory, "*.json").OrderBy(x => x))
            {
                var request = ReadRequest(path);
                TryDelete(path);
                if (request is null ||
                    request.Kind != ShowNotificationKind ||
                    DateTimeOffset.Now - request.CreatedAt > MaximumRequestAge)
                {
                    continue;
                }

                ShowPreviewNotification(request.Settings);
            }
        }
        catch (Exception exception)
        {
            IosStartupDiagnostics.Write("读取悬浮岛提醒预览收件箱失败", exception);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private static HudPreviewNotification? ReadRequest(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<HudPreviewNotification>(
                File.ReadAllText(path), SerializerOptions);
        }
        catch (Exception exception)
        {
            IosStartupDiagnostics.Write($"无法解析悬浮岛提醒预览请求：{path}", exception);
            return null;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // 文件可能已被其它流程处理，忽略即可。
        }
    }

    private static void ShowPreviewNotification(NotificationActionSettings settings)
    {
        var provider = IAppHost.Host?.Services.GetServices<IHostedService>()
            .OfType<ActionNotificationProvider>()
            .FirstOrDefault();
        if (provider == null)
        {
            IosStartupDiagnostics.Write("悬浮岛提醒预览失败：找不到行动提醒提供方");
            return;
        }

        IosStartupDiagnostics.Write($"悬浮岛显示预览提醒：{settings.Mask}");
        _ = ShowNotificationSafeAsync(provider, NotificationAction.BuildNotificationRequest(settings));
    }

    private static async Task ShowNotificationSafeAsync(
        ActionNotificationProvider provider,
        NotificationRequest request)
    {
        try
        {
            await provider.ShowNotificationAsync(request);
        }
        catch (Exception exception)
        {
            IosStartupDiagnostics.Write("显示悬浮岛预览提醒时发生异常", exception);
        }
    }
}
