using ClassIsland.Core.Abstractions.Services.SpeechService;

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