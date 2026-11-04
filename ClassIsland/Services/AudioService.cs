using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core;
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

public class AudioService(ILogger<AudioService> logger) : IAudioService
{
    private readonly AudioEngine _audioEngine = Task.Run((() => new MiniAudioEngine())).Result;
    private ILogger<AudioService> Logger { get; } = logger;

    private RefCounted<AudioPlaybackDevice>? _sharedAudioPlaybackDevice;

    private object _audioPlaybackDeviceInitializeLock = new();

    /// <summary>
    /// 单次音频播放允许占用线程的最长时间。提醒音通常在 1 分钟内结束，
    /// 这里留出充足余量，只用于兜底"设备异常导致 PlaybackEnded 不触发"的情况。
    /// </summary>
    private static readonly TimeSpan PlaybackWaitTimeout = TimeSpan.FromMinutes(2);

    public AudioEngine AudioEngine
    {
        get
        {
            if (OperatingSystem.IsWindows() && Thread.CurrentThread.GetApartmentState() != ApartmentState.MTA)
            {
                throw new InvalidOperationException(
                    "出于线程安全考虑，禁止在非 MTA 线程上调用 AudioEngine。请在 MTA 线程上调用 AudioEngine。详细请见 https://github.com/ClassIsland/ClassIsland/issues/1333#issuecomment-3505591836");
            }
            return _audioEngine;
        }
    }

    public AudioPlaybackDevice? TryInitializeDefaultPlaybackDevice() =>
        TryInitializeDefaultPlaybackDeviceAsync().Result;

    public Task<AudioPlaybackDevice?> TryInitializeDefaultPlaybackDeviceAsync() => 
        Task.Run(TryInitializeDefaultPlaybackDeviceInternal);

    public Task<RefCounted<AudioPlaybackDevice>.Lease?> TryInitializeDefaultPlaybackDeviceSafeAsync() => Task.Run(() =>
    {
        lock (_audioPlaybackDeviceInitializeLock)
        {
            if (_sharedAudioPlaybackDevice?.IsValueDisposed == false)
            {
                var lease = _sharedAudioPlaybackDevice.Rent();
                Logger.LogDebug("使用了缓存的音频设备 {} (Id={})", lease.Value.Info?.Name, lease.Value.Info?.Id);
                return lease;
            }

            if (TryInitializeDefaultPlaybackDeviceInternal() is not { } device)
            {
                return null;
            }
            _sharedAudioPlaybackDevice = new RefCounted<AudioPlaybackDevice>(device);
            var lease2 = _sharedAudioPlaybackDevice.Rent();
            _sharedAudioPlaybackDevice.Dispose();
            return lease2;
        }
    });

    private AudioPlaybackDevice? TryInitializeDefaultPlaybackDeviceInternal()
    {
        try
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
            device.Start();
            return device;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "初始化音频设备失败");
            return null;
        }
        
    }

    public Task PlayAudioAsync(Stream audio, float volume, CancellationToken? cancellationToken = null) => Task.Run(async () =>
    {
        using var audioStream = audio;
        cancellationToken ??= CancellationToken.None;
        using var lease = await TryInitializeDefaultPlaybackDeviceSafeAsync();
        if (lease == null)
        {
            return;
        }

        var device = lease.Value;
        using var player = new SoundPlayer(AudioEngine, IAudioService.DefaultAudioFormat,
            new StreamDataProvider(AudioEngine, IAudioService.DefaultAudioFormat, audio));
        player.Volume = volume;
        Logger.LogDebug("开始播放音频 {}", audio.GetHashCode());
        device.MasterMixer.AddComponent(player);
        var tcs = new TaskCompletionSource<bool>();

        player.PlaybackEnded += OnPlayerOnPlaybackEnded;
        cancellationToken.Value.Register(() =>
        {
            Logger.LogDebug("取消播放音频 {}", audio.GetHashCode());
            tcs.TrySetResult(false);
        });
        player.Play();
        // 不要在此处 await，否则会导致设备停止过程阻塞，无法完成播放流程。
        // 但必须带上限：设备被拔掉、默认设备切换、或音频驱动卡住时 PlaybackEnded 永远
        // 不会触发，旧实现的 tcs.Task.Wait() 会把一个线程池线程永久挂在这里。
        // 一节课接一节课地播放提醒音，线程池就会被逐个耗尽，之后所有 Task.Run
        // （音频、网络、档案保存）都排队等线程，界面表现为"卡死"，要等运行时
        // 慢慢注入新线程才恢复——这正是长时间运行后才卡死的主因之一。
        if (!tcs.Task.Wait(PlaybackWaitTimeout))
        {
            Logger.LogWarning(
                "音频播放等待超时（{Timeout} 秒），已强制结束本次播放并释放播放设备。音频哈希={Hash}",
                PlaybackWaitTimeout.TotalSeconds, audio.GetHashCode());
        }
        Logger.LogDebug("结束播放音频 {}", audio.GetHashCode());
        player.PlaybackEnded -= OnPlayerOnPlaybackEnded;

        return;

        void OnPlayerOnPlaybackEnded(object? sender, EventArgs args)
        {
            tcs.TrySetResult(true);
        }
    });

    public async Task PlayAudioAsync(string filePath, float volume, CancellationToken? cancellationToken = null)
    {
        using var audio = File.OpenRead(filePath);
        await PlayAudioAsync(audio, volume, cancellationToken);
    }

    public void Dispose()
    {
        AudioEngine.Dispose();
        GC.SuppressFinalize(this);
    }
}
