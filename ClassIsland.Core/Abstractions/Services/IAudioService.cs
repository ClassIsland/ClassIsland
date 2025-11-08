using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Structs;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 音频服务，用于提供播放音频的 <see cref="AudioEngine"/>。
/// </summary>
public interface IAudioService : IDisposable
{
    /// <summary>
    /// 默认的音频格式
    /// </summary>
    public static readonly AudioFormat DefaultAudioFormat = AudioFormat.Dvd;
    
    /// <summary>
    /// 当前的音频引擎。
    /// </summary>
    /// <remarks>
    /// 出于线程安全考虑，请从 MTA 线程调用此方法。详细请见 https://github.com/ClassIsland/ClassIsland/issues/1333#issuecomment-3505591836
    /// </remarks>
    /// <exception cref="InvalidOperationException">当调用线程不是 MTA 线程时，抛出此异常。</exception>
    AudioEngine AudioEngine { get; }

    /// <summary>
    /// 尝试初始化默认的音频设备。如果初始化失败，则返回 null。
    /// </summary>
    /// <returns>初始化的音频设备。</returns>
    AudioPlaybackDevice? TryInitializeDefaultPlaybackDevice();
    
    /// <summary>
    /// 尝试初始化默认的音频设备。如果初始化失败，则返回 null。
    /// </summary>
    /// <returns>初始化的音频设备。</returns>
    Task<AudioPlaybackDevice?> TryInitializeDefaultPlaybackDeviceAsync();

    /// <summary>
    /// 播放音频并等待。
    /// </summary>
    /// <param name="audio">音频流</param>
    /// <param name="volume">音频音量</param>
    /// <param name="cancellationToken">用于停止音频播放的取消令牌</param>
    /// <returns></returns>
    Task PlayAudioAsync(Stream audio, float volume, CancellationToken? cancellationToken = null);
}