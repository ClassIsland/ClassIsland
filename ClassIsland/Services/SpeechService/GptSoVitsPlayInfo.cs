using System.Threading;
using System.Threading.Tasks;

namespace ClassIsland.Services.SpeechService;

public class GptSoVitsPlayInfo(string filePath, CancellationTokenSource cts, Task<bool>? downloadTask)
{
    public string FilePath { get; } = filePath;
    public CancellationTokenSource CancellationTokenSource { get; } = cts;
    public Task<bool>? DownloadTask { get; } = downloadTask;
    public bool IsPlayingCompleted { get; set; } = false;
}