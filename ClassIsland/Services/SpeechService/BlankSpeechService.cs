using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Shared.Abstraction.Services;

namespace ClassIsland.Services.SpeechService;

public class BlankSpeechService : ISpeechService
{
    public void EnqueueSpeechQueue(string text)
    {
    }

    public void ClearSpeechQueue()
    {
    }
}