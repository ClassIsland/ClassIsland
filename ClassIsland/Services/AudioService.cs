using System;
using System.Linq;
using ClassIsland.Core.Abstractions.Services;
using Microsoft.Extensions.Logging;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;
using SoundFlow.Structs;

namespace ClassIsland.Services;

public class AudioService(ILogger<AudioDevice> logger) : IAudioService
{
    private ILogger<AudioDevice> Logger { get; } = logger;
    public AudioEngine AudioEngine { get; } = new MiniAudioEngine();
    public AudioPlaybackDevice? TryInitializeDefaultPlaybackDevice()
    {
        var device = AudioEngine.PlaybackDevices.FirstOrDefault(x => x.IsDefault);
        if (device == default)
        {
            Logger.LogDebug("找不到可用的音频设备");
            return null;
        }
        Logger.LogDebug("初始化音频设备 {} (Id={})", device.Name, device.Id);
        return AudioEngine.InitializePlaybackDevice(device, IAudioService.DefaultAudioFormat);
    }

    public void Dispose()
    {
        AudioEngine.Dispose();
        GC.SuppressFinalize(this);
    }
}