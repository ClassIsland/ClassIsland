using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared.Abstraction.Services;

using Edge_tts_sharp;
using Edge_tts_sharp.Model;

using Microsoft.Extensions.Logging;

using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;

namespace ClassIsland.Services.SpeechService;

[SpeechProviderInfo("classisland.speech.edgeTts", "EdgeTTS")]
public class EdgeTtsService : ISpeechService
{
    public static readonly string EdgeTtsCacheFolderPath = Path.Combine(CommonDirectories.AppCacheFolderPath, "EdgeTTS");

    public IAudioService AudioService { get; }
    private ILogger<EdgeTtsService> Logger { get; }

    private SettingsService SettingsService { get; }

    private List<eVoice> Voices { get; } = EdgeTts.GetVoice();

    private Queue<EdgeTtsPlayInfo> PlayingQueue { get; } = new();

    //private AudioPlayer? CurrentPlayer { get; set; }

    private bool IsPlaying { get; set; } = false;

    private EdgeTtsPlayInfo? _currentPlayInfo;
    
    public EdgeTtsService(IAudioService audioService, ILogger<EdgeTtsService> logger, SettingsService settingsService)
    {
        AudioService = audioService;
        Logger = logger;
        SettingsService = settingsService;
        
        Logger.LogInformation("初始化了EdgeTTS服务。");
    }

    private string GetCachePath(string text)
    {
        var data = Encoding.UTF8.GetBytes(text);
        var md5 = MD5.HashData(data);
        var md5String = md5.Aggregate("", (current, t) => current + t.ToString("x2"));
        var path = Path.Combine(EdgeTtsCacheFolderPath, SettingsService.Settings.EdgeTtsVoiceName, $"{md5String}");
        var directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory) && directory != null)
        {
            Directory.CreateDirectory(directory);
        }

        return path;
    }

    public void EnqueueSpeechQueue(string text)
    {
        Logger.LogInformation("以{}朗读文本：{}", SettingsService.Settings.EdgeTtsVoiceName, text);
        var cancellationTokenSource = new CancellationTokenSource();

        var cache = GetCachePath(text);
        Logger.LogDebug("语音缓存：{}", cache);

        Task<bool>? task = null;
        if (!File.Exists(cache))
        {
            task = GenerateSpeechAsync(text, cache, cancellationTokenSource.Token);
        }
        
        if (cancellationTokenSource.IsCancellationRequested)
        {
            cancellationTokenSource.Dispose();
            return;
        }

        PlayingQueue.Enqueue(new EdgeTtsPlayInfo(cache, cancellationTokenSource, task));
        _ = ProcessPlayerList();
    }

    public void ClearSpeechQueue()
    {
        _currentPlayInfo?.CancellationTokenSource.Cancel();
        while (PlayingQueue.Count > 0)
        {
            var playInfo = PlayingQueue.Dequeue();
            playInfo.CancellationTokenSource.Cancel();
            playInfo.CancellationTokenSource.Dispose();
        }
    }

    private async Task ProcessPlayerList()
    {
        if (IsPlaying)
            return;
        IsPlaying = true;

        try
        {
            while (PlayingQueue.Count > 0)
            {
                var playInfo = _currentPlayInfo = PlayingQueue.Dequeue();
                try
                {
                    if (playInfo.CancellationTokenSource.IsCancellationRequested)
                        continue;
                    if (playInfo.DownloadTask != null)
                    {
                        Logger.LogDebug("等待下载完成");
                        var result = await playInfo.DownloadTask;
                        if (!result)
                        {
                            Logger.LogDebug("语音缓存生成未完成：{}", playInfo.FilePath);
                            continue;
                        }
                        Logger.LogDebug("等待下载完成结束");
                    }

                    if (!File.Exists(playInfo.FilePath))
                    {
                        Logger.LogWarning("找不到语音缓存文件：{}", playInfo.FilePath);
                        continue;
                    }

                    Logger.LogDebug("开始播放 {}", playInfo.FilePath);
                    await AudioService.PlayAudioAsync(playInfo.FilePath,
                        (float)SettingsService.Settings.SpeechVolume, playInfo.CancellationTokenSource.Token);
                    Logger.LogDebug("结束播放 {}", playInfo.FilePath);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "无法播放语音。");
                }
                finally
                {
                    _currentPlayInfo = null;
                    playInfo.CancellationTokenSource.Dispose();
                }
            }
        }
        finally
        {
            _currentPlayInfo = null;
            IsPlaying = false;
        }
    }

    private async Task<bool> GenerateSpeechAsync(string text, string cache, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return false;

        var voice = Voices.Find(voice => voice.ShortName == SettingsService.Settings.EdgeTtsVoiceName);
        if (voice == null)
        {
            Logger.LogWarning("找不到 EdgeTTS 语音配置：{}", SettingsService.Settings.EdgeTtsVoiceName);
            return false;
        }

        var completedSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => completedSource.TrySetCanceled(cancellationToken));

        try
        {
            var options = new PlayOption()
            {
                Text = text
            };
            var invokeTask = EdgeTts.InvokeAsync(options, voice, (Action<List<byte>>)(binary =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completedSource.TrySetCanceled(cancellationToken);
                    return;
                }

                try
                {
                    File.WriteAllBytes(cache, binary.ToArray());
                    completedSource.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    completedSource.TrySetException(ex);
                }
            }));
            var completedTask =
                await Task.WhenAny(completedSource.Task, invokeTask, Task.Delay(TimeSpan.FromSeconds(15)));

            if (completedTask == completedSource.Task)
            {
                return await completedSource.Task;
            }

            if (completedTask == invokeTask)
            {
                await invokeTask;
                if (completedSource.Task.IsCompleted)
                {
                    return await completedSource.Task;
                }

                Logger.LogWarning("EdgeTTS 调用已结束，但未生成语音缓存：{}", cache);
                return false;
            }

            Logger.LogWarning("获取EdgeTTS语音超时。");
            return false;
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("已取消获取 EdgeTTS 语音：{}", text);
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取EdgeTTS语音失败。");
            return false;
        }
    }
}
