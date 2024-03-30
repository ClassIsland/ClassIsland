namespace ClassIsland.Core.Services;

public interface ISpeechService
{
    public void EnqueueSpeechQueue(string text);

    public void ClearSpeechQueue();
}