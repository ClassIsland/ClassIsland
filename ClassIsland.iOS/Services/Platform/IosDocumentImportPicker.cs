using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Foundation;
using Microsoft.Extensions.Logging;
using ObjCRuntime;
using UIKit;
using UniformTypeIdentifiers;

namespace ClassIsland.iOS.Services.Platform;

/// <summary>
/// 请求系统交付文件副本，再交给现有的沙盒暂存流程。
/// </summary>
internal static class IosDocumentImportPicker
{
    public static async Task<IReadOnlyList<IStorageFile>> OpenAsync(
        FilePickerOpenOptions options,
        TopLevel root,
        ILogger? logger)
    {
        Dispatcher.UIThread.VerifyAccess();
        var handle = root.TryGetPlatformHandle()
                     ?? throw new InvalidOperationException("The iOS view handle is unavailable.");
        var view = Runtime.GetNSObject<UIView>(handle.Handle);
        var controller = view?.Window?.RootViewController
                         ?? throw new InvalidOperationException("The iOS root controller is unavailable.");
        while (controller.PresentedViewController is { } presented)
        {
            controller = presented;
        }

        var types = GetContentTypes(options.FileTypeFilter);
        logger?.LogInformation("iOS file import: presenting copy picker with types {ContentTypes}",
            string.Join(", ", types.Select(type => type.Identifier)));

        // 应用只消费副本，不要求文件提供方交付可原位编辑的文档。
        using var picker = new UIDocumentPickerViewController(types, asCopy: true)
        {
            AllowsMultipleSelection = options.AllowMultiple,
            Title = options.Title
        };
        using var startLocation = options.SuggestedStartLocation is { } folder
            ? (NSUrl?)folder.Path
            : null;
        picker.DirectoryUrl = startLocation;
        var completion = new TaskCompletionSource<NSUrl[]>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var pickerDelegate = new ImportDelegate(completion, logger);
        using var dismissalDelegate = new DismissalDelegate(completion, logger);
        picker.Delegate = pickerDelegate;
        if (picker.PresentationController is { } presentation)
        {
            presentation.Delegate = dismissalDelegate;
        }

        controller.PresentViewController(picker, true, null);
        var urls = await completion.Task;
        var files = new List<IStorageFile>(urls.Length);
        try
        {
            foreach (var url in urls)
            {
                var uri = (Uri?)url
                          ?? throw new IOException("The imported document URL is invalid.");
                var file = await root.StorageProvider.TryGetFileFromPathAsync(uri)
                           ?? throw new IOException("The imported document copy is unavailable.");
                files.Add(file);
            }

            return files;
        }
        catch
        {
            foreach (var file in files)
            {
                file.Dispose();
            }

            throw;
        }
    }

    private static UTType[] GetContentTypes(IReadOnlyList<FilePickerFileType>? filters)
    {
        var types = new List<UTType>();
        foreach (var filter in filters ?? [])
        {
            var extensions = filter.Patterns?.Select(Path.GetExtension)
                .OfType<string>()
                .Where(extension => extension.StartsWith('.') && !extension.Contains('*'))
                .Select(extension => extension.TrimStart('.'))
                .ToArray();
            if (extensions is { Length: > 0 })
            {
                types.AddRange(extensions.Select(UTType.CreateFromExtension).OfType<UTType>());
            }
            else if (filter.AppleUniformTypeIdentifiers?.Any() == true)
            {
                types.AddRange(filter.AppleUniformTypeIdentifiers
                    .Select(UTType.CreateFromIdentifier).OfType<UTType>());
            }
            else if (filter.MimeTypes?.Any() == true)
            {
                types.AddRange(filter.MimeTypes.Select(UTType.CreateFromMimeType).OfType<UTType>());
            }
        }

        return types.Count == 0 ? [UTTypes.Item] : types.ToArray();
    }

    private sealed class ImportDelegate(
        TaskCompletionSource<NSUrl[]> completion,
        ILogger? logger) : UIDocumentPickerDelegate
    {
        public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl[] urls)
        {
            logger?.LogInformation("iOS file import: native selection callback received {FileCount} files", urls.Length);
            completion.TrySetResult(urls);
        }

        public override void WasCancelled(UIDocumentPickerViewController controller)
        {
            logger?.LogInformation("iOS file import: native picker cancelled");
            completion.TrySetResult([]);
        }
    }

    private sealed class DismissalDelegate(
        TaskCompletionSource<NSUrl[]> completion,
        ILogger? logger) : UIAdaptivePresentationControllerDelegate
    {
        public override void DidDismiss(UIPresentationController presentationController)
        {
            logger?.LogInformation("iOS file import: native picker dismissed");
            completion.TrySetResult([]);
        }
    }
}
