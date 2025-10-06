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
    AudioEngine AudioEngine { get; }

    /// <summary>
    /// 尝试初始化默认的音频设备。如果初始化失败，则返回 null。
    /// </summary>
    /// <returns>初始化的音频设备。</returns>
    AudioPlaybackDevice? TryInitializeDefaultPlaybackDevice();
}