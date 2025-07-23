using System;
using ClassIsland.Core.Abstractions.Services;
using SoundFlow.Abstracts;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;

namespace ClassIsland.Services;

public class AudioService : IAudioService
{
    public AudioEngine AudioEngine { get; } = new MiniAudioEngine(48000, Capability.Playback);

    public void Dispose()
    {
        AudioEngine.Dispose();
        GC.SuppressFinalize(this);
    }
}