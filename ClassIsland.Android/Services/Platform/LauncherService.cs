using Android.Content;
using Android.Content.PM;
using Android.Provider;
using Avalonia.Platform.Storage;
using ClassIsland.Android.Storage;
using ClassIsland.Core;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Android.Services.Platform;

public class LauncherService : ILauncherService
{
    public async Task LaunchPath(string path)
    {
        var topLevel = AppBase.Current.PhonyRootWindow;

        if (PlatformServices.FilePickerService.IsBookmark(path))
        {
            var file = await PlatformServices.FilePickerService.GetFileAsync(path, topLevel) as IStorageItem
                       ?? await PlatformServices.FilePickerService.GetFolderAsync(path, topLevel);
            if (file != null)
            {
                await topLevel.Launcher.LaunchFileAsync(file);
                return;
            }
        }

        var fullPath = Path.GetFullPath(path);
        var appDataPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(CommonDirectories.AppRootFolderPath));
        if (IsSameOrDescendant(fullPath, appDataPath))
        {
            var targetDirectory = File.Exists(fullPath) ? Path.GetDirectoryName(fullPath)! : fullPath;
            var relativePath = Path.GetRelativePath(appDataPath, targetDirectory);
            var providerPath = relativePath == "." ? "Data" : Path.Combine("Data", relativePath);
            var authority = $"{Application.Instance.PackageName}.documents";
            var documentUri = ClassIslandDocumentsProvider.BuildDocumentUri(authority, providerPath);

            using var intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(documentUri, DocumentsContract.Document.MimeTypeDir);
            intent.AddFlags(ActivityFlags.NewTask |
                            ActivityFlags.GrantReadUriPermission |
                            ActivityFlags.GrantWriteUriPermission |
                            ActivityFlags.GrantPrefixUriPermission);
            TargetSystemFileManager(intent);

            try
            {
                Application.Instance.StartActivity(intent);
            }
            catch (ActivityNotFoundException)
            {
                using var fallbackIntent = new Intent(Intent.ActionOpenDocumentTree);
                fallbackIntent.PutExtra(DocumentsContract.ExtraInitialUri, documentUri);
                fallbackIntent.AddFlags(ActivityFlags.NewTask |
                                        ActivityFlags.GrantReadUriPermission |
                                        ActivityFlags.GrantWriteUriPermission |
                                        ActivityFlags.GrantPrefixUriPermission);
                Application.Instance.StartActivity(fallbackIntent);
            }
        }
    }

    public async Task LaunchUrl(string url)
    {
        var topLevel = AppBase.Current.PhonyRootWindow;
        await topLevel.Launcher.LaunchUriAsync(new Uri(url));
    }

    private static bool IsSameOrDescendant(string path, string rootPath)
    {
        if (string.Equals(path, rootPath, StringComparison.Ordinal))
        {
            return true;
        }

        var rootPrefix = rootPath + Path.DirectorySeparatorChar;
        return path.StartsWith(rootPrefix, StringComparison.Ordinal);
    }

    private static void TargetSystemFileManager(Intent intent)
    {
        var packageManager = Application.Instance.PackageManager;
        var fileManagerActivity = packageManager
            ?.QueryIntentActivities(intent, PackageManager.ResolveInfoFlags.Of(0))
            .Select(info => info.ActivityInfo)
            .FirstOrDefault(activity => activity?.PackageName != null &&
                                        packageManager.CheckPermission(
                                            global::Android.Manifest.Permission.ManageDocuments,
                                            activity.PackageName) == Permission.Granted);

        if (fileManagerActivity?.PackageName != null && fileManagerActivity.Name != null)
        {
            intent.SetClassName(fileManagerActivity.PackageName, fileManagerActivity.Name);
        }
    }
}
