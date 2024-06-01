namespace ClassIsland.Shared.Abstraction.Services;

public interface ISpeechService
{
    public void EnqueueSpeechQueue(string text);

    public void ClearSpeechQueue();
}