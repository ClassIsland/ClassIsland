using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ClassIsland.Services.Management;
using ClassIsland.Services.SpeechService;

using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services;

public class FileFolderService : IHostedService
{
    private static List<string> Folders = new()
    {
        App.AppDataFolderPath,
        ManagementService.ManagementConfigureFolderPath,
        "./Temp",
        App.AppCacheFolderPath,
        EdgeTtsService.EdgeTtsCacheFolderPath,
        PluginService.PluginsPkgRootPath,
        PluginService.PluginsRootPath,
        PluginService.PluginConfigsFolderPath,
        PluginService.PluginsIndexPath
    };

    public FileFolderService()
    {
        
    }

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

    /// <summary>
    /// 同名移动文件或目录。
    /// </summary>
    /// <param name="source">文件或目录</param>
    /// <param name="destinationDir">目标根目录</param>
    /// <param name="args">重启ClassIsland的启动参数</param>
    public static void Move(string source, string destinationDir, ref List<string> args)
    {
        Directory.CreateDirectory(destinationDir);
        if (File.Exists(source))
            MoveFile(new FileInfo(source), destinationDir, ref args);
        else if (Directory.Exists(source))
        {
            var dir = new DirectoryInfo(source);
            var dirs = dir.GetDirectories();
            destinationDir = Path.Combine(destinationDir, dir.Name);
            Directory.CreateDirectory(destinationDir);

            foreach (var file in dir.GetFiles())
                MoveFile(file, destinationDir, ref args);

            foreach (var subDir in dirs)
                Move(subDir.FullName, destinationDir, ref args);

            try
            {
                dir.Delete();
            } catch (Exception)
            {
                // delete exception
            }
        }
    }

    static void MoveFile(FileInfo file, string destinationDir, ref List<string> args)
    {
        var targetFilePath = Path.Combine(destinationDir, file.Name);
        file.CopyTo(targetFilePath, true);
        try
        {
            file.Delete();
        } catch (Exception)
        {
            // delete exception
        }
    }
}