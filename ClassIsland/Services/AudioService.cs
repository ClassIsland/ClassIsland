using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using Microsoft.Extensions.Logging;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;

namespace ClassIsland.Services;

public class AudioService(ILogger<AudioDevice> logger) : IAudioService
{
    private readonly AudioEngine _audioEngine = Task.Run((() => new MiniAudioEngine())).Result;
    private ILogger<AudioDevice> Logger { get; } = logger;

    public AudioEngine AudioEngine
    {
        get
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.MTA)
            {
                throw new InvalidOperationException(
                    "出于线程安全考虑，禁止在非 MTA 线程上调用 AudioEngine。请在 MTA 线程上调用 AudioEngine。详细请见 https://github.com/ClassIsland/ClassIsland/issues/1333#issuecomment-3505591836");
            }
            return _audioEngine;
        }
    }

    public AudioPlaybackDevice? TryInitializeDefaultPlaybackDevice() =>
        TryInitializeDefaultPlaybackDeviceAsync().Result;

    public Task<AudioPlaybackDevice?> TryInitializeDefaultPlaybackDeviceAsync() => Task.Run((() =>
    {
        var deviceInfo = AudioEngine.PlaybackDevices.FirstOrDefault(x => x.IsDefault);
        if (deviceInfo == default)
        {
            Logger.LogDebug("找不到可用的音频设备");
            return null;
        }
        Logger.LogDebug("初始化音频设备 {} (Id={})", deviceInfo.Name, deviceInfo.Id);
        var device = AudioEngine.InitializePlaybackDevice(deviceInfo, IAudioService.DefaultAudioFormat);
        device.MasterMixer.Volume = 1.0f;
        return device;
    }));

    public Task PlayAudioAsync(Stream audio, float volume, CancellationToken? cancellationToken = null) => Task.Run(async () =>
    {
        cancellationToken ??= CancellationToken.None;
        using var device = await TryInitializeDefaultPlaybackDeviceAsync();
        if (device == null)
        {
            return;
        }
        device.Start();
        using var player = new SoundPlayer(AudioEngine, IAudioService.DefaultAudioFormat,
            new StreamDataProvider(AudioEngine, IAudioService.DefaultAudioFormat, audio));
        player.Volume = volume;
        Logger.LogDebug("开始播放音频 {}", audio.GetHashCode());
        device?.MasterMixer.AddComponent(player);
        var tcs = new TaskCompletionSource<bool>();

        player.PlaybackEnded += OnPlayerOnPlaybackEnded;
        cancellationToken.Value.Register(() =>
        {
            Logger.LogDebug("取消播放音频 {}", audio.GetHashCode());
            tcs.TrySetResult(false);
        });
        player.Play();
        tcs.Task.Wait();  // 不要在此处 await，否则会导致设备停止过程阻塞，无法完成播放流程。
        Logger.LogDebug("结束播放音频 {}", audio.GetHashCode());
        player.PlaybackEnded -= OnPlayerOnPlaybackEnded;
        
        return;

        void OnPlayerOnPlaybackEnded(object? sender, EventArgs args)
        {
            tcs.TrySetResult(true);
        }
    });

    public void Dispose()
    {
        AudioEngine.Dispose();
        GC.SuppressFinalize(this);
    }
}