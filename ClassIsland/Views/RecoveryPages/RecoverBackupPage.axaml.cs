using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services;
using ClassIsland.ViewModels.RecoveryPages;
using FluentAvalonia.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Path = System.IO.Path;

namespace ClassIsland.Views.RecoveryPages;

/// <summary>
/// RecoverBackupPage.xaml 的交互逻辑
/// </summary>
public partial class RecoverBackupPage : UserControl
{
    private static readonly string[] RecoverableFileNames =
    [
        "Settings.json",
        "Settings.json.bak"
    ];

    private static readonly string[] RecoverableDirectoryNames =
    [
        "Config",
        "Profiles"
    ];

    public FAFrame? MainFrame { get; init; }

    public UserControl? LastPage { get; init; }

    public RecoverBackupViewModel ViewModel { get; } = new();
    public RecoverBackupPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void RecoverBackupPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        var backupPath = Path.Combine(CommonDirectories.AppRootFolderPath, "Backups");

        if (Directory.Exists(backupPath))
        {
            var excludeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) // 忽略指定的文件
            {
                ".DS_Store",
                "Thumbs.db",
                "desktop.ini"
            };

            IEnumerable<string?> files = Directory.GetFiles(backupPath)
                 .Where(file => string.Equals(
                     Path.GetExtension(file),
                     ".zip",
                     StringComparison.OrdinalIgnoreCase))
                 .Where(file => !excludeFiles.Contains(Path.GetFileName(file)))
                 .OrderByDescending(File.GetLastWriteTime)
                 .Select(Path.GetFileName);

            IEnumerable<string?> directories = Directory.GetDirectories(backupPath)
                .OrderByDescending(Directory.GetLastWriteTime)
                .Select(Path.GetFileName);

            ViewModel.Backups = new ObservableCollection<string>(files.Concat(directories));
        }
    }

    private async Task RecoverBackupAsync(string backupPath)
    {
        var fullRecovery = ViewModel.RecoverMode == 1;
        string? stagingPath = null;
        try
        {
            await Task.Run(() =>
            {
                stagingPath = Directory.CreateTempSubdirectory(
                    "ClassIslandBackupRecovery-").FullName;
                MaterializeBackup(backupPath, stagingPath);
                EnsureBackupContainsRecoverableData(stagingPath);
                AppDataConfigurationValidator.ValidateAvailable(stagingPath);
                ExecuteRecoveryTransaction(stagingPath, fullRecovery);
            });
        }
        finally
        {
            FileSystemDataTransaction.TryDeleteDirectory(stagingPath);
        }
    }

    private static void ExecuteRecoveryTransaction(
        string stagingPath,
        bool fullRecovery)
    {
        var appRoot = Path.GetFullPath(CommonDirectories.AppRootFolderPath);
        Directory.CreateDirectory(appRoot);
        var appTransactionPaths = GetPresentAppDataPaths(stagingPath);

        void ExecuteAppTransaction()
        {
            if (appTransactionPaths.Count == 0)
            {
                CopyImportedFilesIfPresent(stagingPath);
                return;
            }

            var rollbackPath = Directory.CreateTempSubdirectory(
                "ClassIslandBackupRollback-").FullName;
            FileSystemDataTransaction.Execute(
                appRoot,
                rollbackPath,
                appTransactionPaths,
                () =>
                {
                    if (fullRecovery)
                    {
                        DeleteRecoverableAppData(
                            appRoot,
                            appTransactionPaths);
                    }

                    CopyRecoverableAppData(stagingPath, appRoot, true);
                    CopyImportedFilesIfPresent(stagingPath);
                });
        }

        var stagedImportedFiles = Path.Combine(stagingPath, "ImportedFiles");
        if (!Directory.Exists(stagedImportedFiles))
        {
            ExecuteAppTransaction();
            return;
        }

        var importedRoot = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(CommonDirectories.AppImportedFilesFolderPath));
        var importedParent = Path.GetDirectoryName(importedRoot)
                             ?? throw new InvalidDataException(
                                 "无法确定持久导入文件目录的父目录。");
        Directory.CreateDirectory(importedParent);
        var importedRollbackPath = Directory.CreateTempSubdirectory(
            "ClassIslandImportedFilesRollback-").FullName;
        FileSystemDataTransaction.Execute(
            importedParent,
            importedRollbackPath,
            [Path.GetFileName(importedRoot)],
            () =>
            {
                if (fullRecovery)
                {
                    FileSystemDataTransaction.DeleteEntry(importedRoot);
                }

                ExecuteAppTransaction();
            });
    }

    private static void MaterializeBackup(
        string backupPath,
        string stagingPath)
    {
        if (File.Exists(backupPath))
        {
            if (!string.Equals(
                    Path.GetExtension(backupPath),
                    ".zip",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "备份文件不是有效的 ZIP 归档。");
            }

            using var archive = ZipFile.OpenRead(backupPath);
            ZipArchiveSafety.ValidateForClassIslandDataExtraction(archive);
            SafeArchiveExtractor.ExtractSelected(
                archive,
                stagingPath,
                new HashSet<string>(
                    RecoverableFileNames,
                    StringComparer.Ordinal),
                new HashSet<string>(
                    RecoverableDirectoryNames.Append("ImportedFiles"),
                    StringComparer.Ordinal));
            return;
        }

        if (Directory.Exists(backupPath))
        {
            CopyRecoverableAppData(backupPath, stagingPath, true);
            var importedFilesSource = Path.Combine(
                backupPath,
                "ImportedFiles");
            if (Directory.Exists(importedFilesSource))
            {
                FileFolderService.CopyFolderStrict(
                    importedFilesSource,
                    Path.Combine(stagingPath, "ImportedFiles"),
                    true);
            }

            return;
        }

        throw new FileNotFoundException("找不到要恢复的备份。", backupPath);
    }

    private static void EnsureBackupContainsRecoverableData(string root)
    {
        if (GetPresentAppDataPaths(root).Count > 0 ||
            Directory.Exists(Path.Combine(root, "ImportedFiles")))
        {
            return;
        }

        throw new InvalidDataException(
            "备份中不包含可恢复的 ClassIsland 配置数据。");
    }

    private static void CopyRecoverableAppData(
        string sourceRoot,
        string destinationRoot,
        bool overwrite)
    {
        Directory.CreateDirectory(destinationRoot);
        foreach (var name in RecoverableFileNames)
        {
            var source = Path.Combine(sourceRoot, name);
            if (File.Exists(source))
            {
                FileSystemDataTransaction.CopyFileStrict(
                    source,
                    Path.Combine(destinationRoot, name),
                    overwrite);
            }
        }

        foreach (var name in RecoverableDirectoryNames)
        {
            var source = Path.Combine(sourceRoot, name);
            if (Directory.Exists(source))
            {
                FileFolderService.CopyFolderStrict(
                    source,
                    Path.Combine(destinationRoot, name),
                    overwrite);
            }
        }
    }

    private static void CopyImportedFilesIfPresent(string stagingPath)
    {
        var source = Path.Combine(stagingPath, "ImportedFiles");
        if (Directory.Exists(source))
        {
            FileFolderService.CopyFolderStrict(
                source,
                CommonDirectories.AppImportedFilesFolderPath,
                true);
        }
    }

    private static IReadOnlyCollection<string> GetPresentAppDataPaths(
        string root)
    {
        var paths = new List<string>();
        if (RecoverableFileNames.Any(name =>
                File.Exists(Path.Combine(root, name))))
        {
            paths.AddRange(RecoverableFileNames);
        }

        foreach (var name in RecoverableDirectoryNames)
        {
            if (Directory.Exists(Path.Combine(root, name)))
            {
                paths.Add(name);
            }
        }

        return paths;
    }

    private static void DeleteRecoverableAppData(
        string root,
        IEnumerable<string> relativePaths)
    {
        foreach (var relativePath in relativePaths)
        {
            FileSystemDataTransaction.DeleteEntry(
                Path.Combine(root, relativePath));
        }
    }

    private async void ButtonRecover_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedBackupName == null)
        {
            return;
        }

        var result = await ContentDialogHelper.ShowConfirmationDialog("恢复备份",
            $"您确定要把应用配置恢复到备份 {ViewModel.SelectedBackupName} 的状态吗？此操作无法撤销。",
            root: TopLevel.GetTopLevel(this));
        if (!result)
        {
            return;
        }

        var backupPath = SafeChildDirectoryPath.Resolve(
            Path.Combine(CommonDirectories.AppRootFolderPath, "Backups"),
            ViewModel.SelectedBackupName);

        try
        {
            ViewModel.IsWorking = true;
            await RecoverBackupAsync(backupPath);
            this.ShowSuccessToast("操作成功完成。");
        }
        catch (Exception exception)
        {
            this.ShowErrorToast("无法恢复备份", exception);
        }
        finally
        {
            ViewModel.IsWorking = false;
        }
    }

    private void ButtonGoBack_OnClick(object? sender, RoutedEventArgs e)
    {
        if (MainFrame != null)
        {
            MainFrame.Content = LastPage;
        }
    }
}
