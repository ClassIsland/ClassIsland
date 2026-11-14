using Avalonia.Controls;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Helpers;
using ClassIsland.Platforms.Abstraction.Services;
using Foundation;
using ObjCRuntime;
using QuickLook;
using UIKit;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 使用 iOS/iPadOS 系统界面打开目录、预览文件和打开外部链接。
/// </summary>
internal sealed class IosLauncherService : ILauncherService
{
    private readonly SharedDocumentsLauncherService _service = new(
        GetDocumentsPath,
        IosSystemUrlOpener.OpenAsync);

    private QLPreviewController? _previewController;
    private FilePreviewDataSource? _previewDataSource;

    public Task LaunchPath(string path)
    {
        path = ImportedFileReference.Resolve(path);
        if (File.Exists(path))
        {
            path = Path.GetDirectoryName(path)
                   ?? throw new DirectoryNotFoundException(
                       "无法获取所选文件的父目录。");
        }

        return _service.LaunchPath(path);
    }

    public Task LaunchUrl(string url) => _service.LaunchUrl(url);

    public Task LaunchAppLink(string url) => _service.LaunchAppLink(url);

    public async Task LaunchFile(string path)
    {
        path = ImportedFileReference.Resolve(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("找不到要预览的文件，请在行动设置中重新选择文件。");
        }

        var fullPath = Path.GetFullPath(path);
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (UIApplication.SharedApplication.ApplicationState != UIApplicationState.Active)
            {
                throw new InvalidOperationException("请返回 ClassIsland 后重新运行文件预览行动。");
            }

            var root = AppBase.Current.GetRootWindow();
            var handle = root.TryGetPlatformHandle();
            var view = handle == null ? null : Runtime.GetNSObject<UIView>(handle.Handle);
            var controller = view?.Window?.RootViewController
                             ?? throw new InvalidOperationException("暂时无法显示文件预览，请返回主界面后重试。");
            if (controller.PresentedViewController != null || _previewController != null)
            {
                throw new InvalidOperationException("请先关闭当前系统窗口或文件预览，再运行此行动。");
            }

            // Quick Look 的 dataSource 为弱引用，必须保留到预览关闭。
            _previewDataSource = new FilePreviewDataSource(fullPath);
            _previewController = new QLPreviewController { DataSource = _previewDataSource };
            _previewController.DidDismiss += OnPreviewDismissed;
            try
            {
                await controller.PresentViewControllerAsync(_previewController, true);
            }
            catch
            {
                ClearPreview();
                throw;
            }
        });
    }

    private void OnPreviewDismissed(object? sender, EventArgs e)
    {
        // 避免在原生回调仍使用控制器时释放它。
        Dispatcher.UIThread.Post(ClearPreview);
    }

    private void ClearPreview()
    {
        if (_previewController != null)
        {
            _previewController.DidDismiss -= OnPreviewDismissed;
            _previewController.Dispose();
            _previewController = null;
        }

        _previewDataSource?.Dispose();
        _previewDataSource = null;
    }

    private sealed class FilePreviewDataSource(string path) : QLPreviewControllerDataSource
    {
        private readonly FilePreviewItem _item = new(path);

        public override nint PreviewItemCount(QLPreviewController controller) => 1;

        public override IQLPreviewItem GetPreviewItem(QLPreviewController controller, nint index) => _item;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _item.Dispose();
            base.Dispose(disposing);
        }
    }

    private sealed class FilePreviewItem(string path) : QLPreviewItem
    {
        private readonly NSUrl _url = NSUrl.FromFilename(path);

        public override NSUrl PreviewItemUrl => _url;

        public override string PreviewItemTitle => Path.GetFileName(path);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _url.Dispose();
            base.Dispose(disposing);
        }
    }

    private static string GetDocumentsPath()
    {
        var documentsUrl = NSFileManager.DefaultManager
            .GetUrls(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("无法获取 iOS Documents 目录。");
        return documentsUrl.Path
               ?? throw new InvalidOperationException("无法获取 iOS Documents 目录路径。");
    }
}
