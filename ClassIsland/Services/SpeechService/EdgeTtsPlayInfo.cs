using System.Threading;
using System.Threading.Tasks;

namespace ClassIsland.Services.SpeechService;

public class EdgeTtsPlayInfo(string filePath, CancellationTokenSource source, Task? downloadTask)
{
    public string FilePath { get; set; } = filePath;

    public CancellationTokenSource CancellationTokenSource { get; set; } = source;

    public Task? DownloadTask { get; set; } = downloadTask;

    public bool IsPlayingCompleted { get; set; } = false;
}