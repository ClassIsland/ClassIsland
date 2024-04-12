namespace ClassIsland.Core.Abstraction.Services;

public interface ISpeechService
{
    public void EnqueueSpeechQueue(string text);

    public void ClearSpeechQueue();
}