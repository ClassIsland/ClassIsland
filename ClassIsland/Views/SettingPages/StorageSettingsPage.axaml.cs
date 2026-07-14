using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Models;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.ViewModels.SettingsPages;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// StorageSettingsPage.xaml 的交互逻辑
/// </summary>
[Group("classisland.general")]
[SettingsPageInfo("storage", "存储", "\ue6b7", "\ue6b6", SettingsPageCategory.Internal)]
public partial class StorageSettingsPage : SettingsPageBase
{
    private const long MaximumReferenceScanFileLength = 32L * 1024 * 1024;
    private static readonly SemaphoreSlim ImportedFilesCleanupGate = new(1, 1);

    private static readonly HashSet<string>
        ImportedFilesCleanupExcludedDirectories =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Temp",
                "Cache",
                "Logs",
                "Backups"
            };

    public StorageSettingsViewModel ViewModel { get; } = IAppHost.GetService<StorageSettingsViewModel>();

    public ILogger<StorageSettingsPage> Logger => ViewModel.Logger;

    public StorageSettingsPage()
    {
        ViewModel.SettingsService.Settings.BackupFilesSize = Helpers.StorageSizeHelper.FormatSize(Helpers.StorageSizeHelper.GetFolderStorageSize(Path.Combine(CommonDirectories.AppRootFolderPath, "Backups/")));
        DataContext = this;
        InitializeComponent();
        IosImportedFilesSettings.IsVisible = PlatformHelper.IsAppleMobile;
    }

    private async void ButtonCreateBackup_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsBackupFinished = false;
        ViewModel.IsBackingUp = true;
        try
        {
            await FileFolderService.CreateBackupAsync();
            ViewModel.IsBackupFinished = true;
            this.ShowSuccessToast("备份成功。");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "无法创建备份。");
            this.ShowErrorToast("无法创建备份", exception);
        }
        ViewModel.IsBackingUp = false;
    }

    private async void ButtonViewBackupFiles_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await PlatformServices.LauncherService.LaunchPath(
                Path.GetFullPath(Path.Combine(CommonDirectories.AppRootFolderPath, "Backups")));
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "无法浏览备份文件。");
            this.ShowErrorToast($"无法浏览备份文件", exception);
        }
    }

    private async void ButtonRecoverBackup_OnClick(object sender, RoutedEventArgs e)
    {
        if (!await ViewModel.ManagementService.AuthorizeByLevel(ViewModel.ManagementService.CredentialConfig.ExitApplicationAuthorizeLevel))
        {
            return;
        }
        AppBase.Current.Restart(["-m", "-r"]);
    }

    private async void ButtonViewImportedFiles_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(CommonDirectories.AppImportedFilesFolderPath);
            await PlatformServices.LauncherService.LaunchPath(
                CommonDirectories.AppImportedFilesFolderPath);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "无法浏览 iOS 导入文件副本。");
            this.ShowErrorToast("无法浏览导入文件副本", exception);
        }
    }

    private async void ButtonCleanupImportedFiles_OnClick(
        object sender,
        RoutedEventArgs e)
    {
        var confirmed = await ContentDialogHelper.ShowConfirmationDialog(
            "清理未使用的导入文件",
            "ClassIsland 会扫描当前设置、档案和配置，只删除没有持久引用的导入项目。一次性导入文件保存在临时目录，不受影响。",
            root: TopLevel.GetTopLevel(this));
        if (!confirmed)
        {
            return;
        }

        var gateEntered = false;
        try
        {
            await ImportedFilesCleanupGate.WaitAsync();
            gateEntered = true;
            PersistCurrentStateBeforeImportedFilesCleanup();
            Directory.CreateDirectory(
                CommonDirectories.AppImportedFilesFolderPath);
            var firstScan = await Task.Run(() =>
                FindUnreferencedImportedItems());
            if (firstScan.UninspectableSource != null)
            {
                ShowUninspectableSourceWarning(firstScan.UninspectableSource);
                return;
            }

            if (firstScan.UnreferencedDirectoryNames.Count == 0)
            {
                this.ShowSuccessToast("没有发现未使用的导入文件。");
                return;
            }

            // 配置在首次扫描期间可能发生变化。删除前再次持久化，并在同一
            // 后台操作中只删除第二次扫描仍确认未引用的候选项。
            PersistCurrentStateBeforeImportedFilesCleanup();
            var result = await Task.Run(() => RecheckAndDeleteImportedItems(
                firstScan.UnreferencedDirectoryNames));
            if (result.UninspectableSource != null)
            {
                ShowUninspectableSourceWarning(result.UninspectableSource);
            }
            else
            {
                this.ShowSuccessToast(result.DeletedCount == 0
                    ? "没有发现未使用的导入文件。"
                    : $"已清理 {result.DeletedCount} 个未使用的导入文件项目。");
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "无法清理 iOS 导入文件副本。");
            this.ShowErrorToast("无法清理导入文件副本", exception);
        }
        finally
        {
            if (gateEntered)
            {
                ImportedFilesCleanupGate.Release();
            }
        }
    }

    private void ShowUninspectableSourceWarning(string source)
    {
        var relativeSource = Path.GetRelativePath(
            CommonDirectories.AppRootFolderPath,
            source);
        this.ShowWarningToast(
            $"检测到无法安全扫描的配置文件，未执行清理：{relativeSource}");
    }

    private void PersistCurrentStateBeforeImportedFilesCleanup()
    {
        const string note = "清理未使用的 iOS 导入文件前保存当前配置。";
        ViewModel.SettingsService.SaveSettings(note);
        IAppHost.TryGetService<IAutomationService>()?.SaveConfig(note);
        IAppHost.TryGetService<IProfileService>()?.SaveProfile();
        IAppHost.TryGetService<IComponentsService>()?.SaveConfig();
    }

    private static ImportedFilesReferenceScan FindUnreferencedImportedItems(
        IReadOnlySet<string>? restrictedCandidateNames = null)
    {
        var importedRoot = Path.GetFullPath(
            CommonDirectories.AppImportedFilesFolderPath);
        FileSystemDataTransaction.EnsureDirectoryIsNotLink(importedRoot);
        var candidates = Directory.EnumerateDirectories(importedRoot)
            .Where(path => restrictedCandidateNames == null ||
                           restrictedCandidateNames.Contains(
                               Path.GetFileName(path)))
            .Select(path => new
            {
                Path = path,
                PortableReference = ImportedFileReference.Prefix +
                                    Uri.EscapeDataString(Path.GetFileName(path)),
                LegacyMarker = $"/ImportedFiles/{Path.GetFileName(path)}/"
            })
            .ToList();
        foreach (var candidate in candidates)
        {
            FileSystemDataTransaction.EnsureDirectoryIsNotLink(candidate.Path);
        }

        if (candidates.Count == 0)
        {
            return new ImportedFilesReferenceScan([], null);
        }

        var referencedMarkers = new HashSet<string>(StringComparer.Ordinal);
        var searchableExtensions = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ".json", ".json5", ".yaml", ".yml", ".toml", ".xml", ".txt",
            ".ini", ".cfg", ".conf", ".config", ".properties", ".axaml",
            ".xaml", ".svg", ".css", ".md", ".resx"
        };
        var knownBinaryExtensions = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp", ".ico",
            ".avif", ".heic", ".heif", ".wav", ".mp3", ".ogg", ".flac",
            ".m4a", ".aac", ".mp4", ".mov", ".webm", ".ttf", ".otf",
            ".woff", ".woff2", ".dll", ".exe", ".so", ".dylib", ".pdb"
        };
        foreach (var file in EnumerateImportedFileReferenceSources())
        {
            var fullPath = Path.GetFullPath(file);
            FileSystemDataTransaction.EnsureFileIsNotLink(fullPath);
            if (!IsSearchableImportedFileReferenceSource(
                    fullPath,
                    searchableExtensions))
            {
                if (new FileInfo(fullPath).Length == 0 ||
                    knownBinaryExtensions.Contains(Path.GetExtension(fullPath)))
                {
                    continue;
                }

                if (IsInsideConfigurationTree(fullPath))
                {
                    return new ImportedFilesReferenceScan([], fullPath);
                }

                continue;
            }

            string text;
            try
            {
                if (new FileInfo(fullPath).Length >
                    MaximumReferenceScanFileLength)
                {
                    return new ImportedFilesReferenceScan([], fullPath);
                }

                text = File.ReadAllText(fullPath);
            }
            catch (Exception exception)
            {
                throw new IOException(
                    $"无法读取配置文件，已中止导入文件清理：{fullPath}",
                    exception);
            }

            var normalizedText = NormalizeReferenceSeparators(text);
            foreach (var candidate in candidates)
            {
                if (!referencedMarkers.Contains(candidate.PortableReference) &&
                    (normalizedText.Contains(
                         candidate.PortableReference,
                         StringComparison.Ordinal) ||
                     normalizedText.Contains(
                         candidate.LegacyMarker,
                         StringComparison.Ordinal)))
                {
                    referencedMarkers.Add(candidate.PortableReference);
                }
            }
        }

        return new ImportedFilesReferenceScan(
            candidates
                .Where(candidate => !referencedMarkers.Contains(
                    candidate.PortableReference))
                .Select(candidate => Path.GetFileName(candidate.Path))
                .ToArray(),
            null);
    }

    private static ImportedFilesCleanupResult RecheckAndDeleteImportedItems(
        IReadOnlyCollection<string> candidateDirectoryNames)
    {
        var restrictedNames = new HashSet<string>(
            candidateDirectoryNames,
            StringComparer.Ordinal);
        var scan = FindUnreferencedImportedItems(restrictedNames);
        if (scan.UninspectableSource != null)
        {
            return new ImportedFilesCleanupResult(
                0,
                scan.UninspectableSource);
        }

        var importedRoot = Path.GetFullPath(
            CommonDirectories.AppImportedFilesFolderPath);
        var deleted = 0;
        foreach (var directoryName in scan.UnreferencedDirectoryNames)
        {
            var path = SafeChildDirectoryPath.Resolve(
                importedRoot,
                directoryName);
            if (!Directory.Exists(path))
            {
                continue;
            }

            FileSystemDataTransaction.EnsureDirectoryIsNotLink(path);
            FileSystemDataTransaction.DeleteEntry(path);
            deleted++;
        }

        return new ImportedFilesCleanupResult(deleted, null);
    }

    private static bool IsInsideConfigurationTree(string path) =>
        IsSameOrDescendant(path, CommonDirectories.AppConfigPath) ||
        IsSameOrDescendant(
            path,
            Path.Combine(CommonDirectories.AppRootFolderPath, "Profiles"));

    private static bool IsSameOrDescendant(string path, string root)
    {
        var fullPath = Path.GetFullPath(path);
        var fullRoot = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(root));
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return string.Equals(fullPath, fullRoot, comparison) ||
               fullPath.StartsWith(
                   fullRoot + Path.DirectorySeparatorChar,
                   comparison);
    }

    private static bool IsSearchableImportedFileReferenceSource(
        string path,
        IReadOnlySet<string> searchableExtensions)
    {
        if (searchableExtensions.Contains(Path.GetExtension(path)))
        {
            return true;
        }

        return searchableExtensions.Any(extension =>
            path.EndsWith(
                extension + ".bak",
                StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string>
        EnumerateImportedFileReferenceSources()
    {
        var appRoot = Path.GetFullPath(CommonDirectories.AppRootFolderPath);
        foreach (var file in Directory.EnumerateFiles(appRoot))
        {
            yield return file;
        }

        foreach (var directory in Directory.EnumerateDirectories(appRoot))
        {
            if (ImportedFilesCleanupExcludedDirectories.Contains(
                    Path.GetFileName(directory)))
            {
                continue;
            }

            foreach (var file in FileSystemDataTransaction
                         .EnumerateFilesStrict(directory))
            {
                yield return file;
            }
        }
    }

    private static string NormalizeReferenceSeparators(string text)
    {
        var result = new StringBuilder(text.Length);
        var previousWasSeparator = false;
        foreach (var character in text)
        {
            if (character is '/' or '\\')
            {
                if (!previousWasSeparator)
                {
                    result.Append('/');
                }

                previousWasSeparator = true;
                continue;
            }

            result.Append(character);
            previousWasSeparator = false;
        }

        return result.ToString();
    }

    private sealed record ImportedFilesCleanupResult(
        int DeletedCount,
        string? UninspectableSource);

    private sealed record ImportedFilesReferenceScan(
        IReadOnlyList<string> UnreferencedDirectoryNames,
        string? UninspectableSource);
}
