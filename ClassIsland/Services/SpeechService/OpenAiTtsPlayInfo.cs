using System.Threading;
using System.Threading.Tasks;

namespace ClassIsland.Services.SpeechService;

public class OpenAiTtsPlayInfo(string filePath, CancellationTokenSource cts, Task<bool>? generationTask)
{
    public string FilePath { get; } = filePath;
    public CancellationTokenSource CancellationTokenSource { get; } = cts;
    public Task<bool>? GenerationTask { get; } = generationTask;
}
