using SoundFlow.Abstracts;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 音频服务，用于提供播放音频的 <see cref="AudioEngine"/>。
/// </summary>
public interface IAudioService : IDisposable
{
    /// <summary>
    /// 当前的音频引擎。
    /// </summary>
    AudioEngine AudioEngine { get; }
}