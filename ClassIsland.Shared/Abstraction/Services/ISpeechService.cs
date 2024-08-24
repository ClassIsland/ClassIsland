namespace ClassIsland.Shared.Abstraction.Services;

/// <summary>
/// TTS服务接口
/// </summary>
public interface ISpeechService
{
    /// <summary>
    /// 向TTS队列中添加文本。
    /// </summary>
    /// <param name="text">要朗读的文本</param>
    public void EnqueueSpeechQueue(string text);

    /// <summary>
    /// 清空TTS队列。
    /// </summary>
    public void ClearSpeechQueue();
}