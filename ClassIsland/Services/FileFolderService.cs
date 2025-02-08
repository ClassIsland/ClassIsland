using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClassIsland.Services.AppUpdating;
using ClassIsland.Services.Management;
using ClassIsland.Services.SpeechService;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Path = System.IO.Path;

namespace ClassIsland.Services;

public class FileFolderService(SettingsService settingsService, ILogger<FileFolderService> logger) : IHostedService
{
    public SettingsService SettingsService { get; } = settingsService;
    public ILogger<FileFolderService> Logger { get; } = logger;

    private static List<string> Folders =
    [
        App.AppDataFolderPath,
        ManagementService.ManagementConfigureFolderPath,
        App.AppTempFolderPath,
        App.AppCacheFolderPath,
        UpdateService.UpdateCachePath,
        EdgeTtsService.EdgeTtsCacheFolderPath,
        PluginService.PluginsPkgRootPath,
        PluginService.PluginsRootPath,
        PluginService.PluginConfigsFolderPath,
        PluginService.PluginsIndexPath,
        Path.Combine(App.AppRootFolderPath, "Backups"),
        App.AppLogFolderPath,
        AutomationService.AutomationConfigsFolderPath,
        ManagementService.LocalManagementConfigureFolderPath
    ];

    public static void CreateFolders()
    {
        foreach (var i in Folders.Where(i => !Directory.Exists(i)))
        {
            Directory.CreateDirectory(i);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }

    public static void CopyFolder(string source, string destination, bool overwrite=false)
    {
        if (!Directory.Exists(destination))
        {
            Directory.CreateDirectory(destination);
        }
        foreach (var i in Directory.EnumerateFiles(source))
        {
            try
            {
                File.Copy(i, Path.Combine(destination, Path.GetFileName(i)), overwrite);
            }
            catch (Exception e)
            {
                // ignore
            }
        }

        foreach (var i in Directory.EnumerateDirectories(source))
        {
            CopyFolder(Path.Combine(source, Path.GetFileName(i)), Path.Combine(destination, Path.GetFileName(i)));
        }
    }

    public async Task ProcessAutoBackupAsync()
    {
        if (!SettingsService.Settings.IsAutoBackupEnabled)
        {
            return;
        }

        if (!(DateTime.Now.Date - SettingsService.Settings.LastAutoBackupTime.Date >=
              TimeSpan.FromDays(SettingsService.Settings.AutoBackupIntervalDays)))
        {
            return;
        }

        await CreateBackupAsync(true);
        SettingsService.Settings.LastAutoBackupTime = DateTime.Now;

        if (!Directory.Exists(Path.Combine(App.AppRootFolderPath, "Backups")))
        {
            return;
        }

        if (SettingsService.Settings.AutoBackupLimit <= 0)
        {
            return;
        }
        var outdatedBackups = Directory.EnumerateDirectories(Path.Combine(App.AppRootFolderPath, "Backups"), "Auto_*").OrderByDescending(Directory.GetLastWriteTime).Skip(SettingsService.Settings.AutoBackupLimit).ToList();
        foreach (var i in outdatedBackups)
        {
            Directory.Delete(i, true);
        }        
    }

    public static async Task CreateBackupAsync(bool isAuto=false, string? filename=null, string? rootPath=null)
    {
        string[] backupFolders =
        [
            App.AppConfigPath,
            "Profiles/"
        ];
        string[] backupFiles =
        [
            "Settings.json"
        ];
        rootPath ??= App.AppRootFolderPath;
        var backupFolder = Path.Combine(rootPath, "Backups/");
        var backupFilename = string.IsNullOrWhiteSpace(filename) ? $"Backup_{DateTime.Now:yy-MMM-dd_HH-mm-ss}" : filename;
        if (isAuto)
        {
            backupFilename = "Auto_" + backupFilename;
        }

        var backupTarget = Path.Combine(backupFolder, backupFilename);

        if (!Directory.Exists(backupFolder))
        {
            Directory.CreateDirectory(backupFolder);
        }

        if (Directory.Exists(backupTarget))
        {
            Directory.Delete(backupTarget, true);
        }

        Directory.CreateDirectory(backupTarget);

        await Task.Run(() =>
        {
            foreach (var i in backupFiles)
            {
                if (!Path.Exists(Path.Combine(rootPath, i)))
                {
                    continue;
                }
                File.Copy(Path.Combine(rootPath, i), Path.Combine(backupTarget, i));
            }

            foreach (var i in backupFolders)
            {
                if (!Path.Exists(Path.Combine(rootPath, i)))
                {
                    continue;
                }
                CopyFolder(Path.Combine(rootPath, i), Path.Combine(backupTarget, i));
            }
        });
    }
}