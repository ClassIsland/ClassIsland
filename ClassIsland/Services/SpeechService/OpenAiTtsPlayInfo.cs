using System.Threading;
using System.Threading.Tasks;

namespace ClassIsland.Services.SpeechService;

public class OpenAiTtsPlayInfo(string filePath, CancellationTokenSource cts,
    InFlightSpeechGeneration? generation)
{
    public string FilePath { get; } = filePath;
    public CancellationTokenSource CancellationTokenSource { get; } = cts;
    public InFlightSpeechGeneration? Generation { get; } = generation;
    public bool IsGenerationConsumerReleased { get; set; }
}

public class InFlightSpeechGeneration(CancellationTokenSource cancellationTokenSource)
{
    public CancellationTokenSource CancellationTokenSource { get; } = cancellationTokenSource;
    public Task<bool> Task { get; set; } = null!;
    public int ConsumerCount { get; set; }
}
