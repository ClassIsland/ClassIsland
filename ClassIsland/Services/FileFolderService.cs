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
        EdgeTtsService.EdgeTtsCacheFolderPath
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
}