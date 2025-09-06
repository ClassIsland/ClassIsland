using System;
using ClassIsland.Core.Abstractions.Services;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;

namespace ClassIsland.Services;

public class AudioService : IAudioService
{
    public AudioEngine AudioEngine { get; } = new MiniAudioEngine();

    public void Dispose()
    {
        AudioEngine.Dispose();
        GC.SuppressFinalize(this);
    }
}