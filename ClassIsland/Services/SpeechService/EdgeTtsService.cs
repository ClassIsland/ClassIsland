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

    private CancellationTokenSource? requestingCancellationTokenSource;

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
        var previousRequest = requestingCancellationTokenSource;
        previousRequest?.Cancel();
        requestingCancellationTokenSource = new CancellationTokenSource();

        var cache = GetCachePath(text);
        Logger.LogDebug("语音缓存：{}", cache);

        Task? task = null;
        if (!File.Exists(cache))
        {
            task = Task.Run(async () =>
                {
                    var voice = Voices.Find(v => v.ShortName == SettingsService.Settings.EdgeTtsVoiceName);
                    var tcs = new TaskCompletionSource<bool>();
                    try
                    {
                        var options = new PlayOption()
                        {
                            Text = text
                        };
                        EdgeTts.Invoke(options, voice, (Action<List<byte>>)(binary =>
                        {
                            try
                            {
                                if (requestingCancellationTokenSource.IsCancellationRequested)
                                {
                                    tcs.TrySetResult(false);
                                    return;
                                }
                                var dir = Path.GetDirectoryName(cache);
                                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                                {
                                    Directory.CreateDirectory(dir);
                                }
                                var temp = cache + ".tmp";
                                File.WriteAllBytes(temp, binary.ToArray());
                                File.Move(temp, cache, true);
                                tcs.TrySetResult(true);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "语音缓存写入失败: {}", cache);
                                tcs.TrySetResult(false);
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "语音请求失败");
                        tcs.TrySetResult(false);
                    }
                    try
                    {
                        await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(20), requestingCancellationTokenSource.Token));
                    }
                    catch (OperationCanceledException)
                    {
                        // ignore
                    }
                }, requestingCancellationTokenSource.Token);
        }
        
        if (requestingCancellationTokenSource.IsCancellationRequested)
            return;
        PlayingQueue.Enqueue(new EdgeTtsPlayInfo(cache, new CancellationTokenSource(), task));
        _ = ProcessPlayerList();
    }

    public void ClearSpeechQueue()
    {
        requestingCancellationTokenSource?.Cancel();
        _currentPlayInfo?.CancellationTokenSource.Cancel();
        foreach (var pair in PlayingQueue)
        {
            pair.CancellationTokenSource.Cancel();
        }
        //IsPlaying = false;
    }

    private async Task ProcessPlayerList()
    {
        if (IsPlaying)
            return;
        IsPlaying = true;


        while (PlayingQueue.Count > 0)
        {
            var playInfo = _currentPlayInfo = PlayingQueue.Dequeue();
            if (playInfo.CancellationTokenSource.IsCancellationRequested)
                continue;
            if (playInfo.DownloadTask != null)
            {
                Logger.LogDebug("等待下载完成");
                await playInfo.DownloadTask;
                Logger.LogDebug("等待下载完成结束");
            }
            if (!File.Exists(playInfo.FilePath))
            {
                Logger.LogWarning("语音缓存不存在: {}", playInfo.FilePath);
                continue;
            }

            try
            {
                
                Logger.LogDebug("开始播放 {}", playInfo.FilePath);
                await AudioService.PlayAudioAsync(File.OpenRead(playInfo.FilePath),
                    (float)SettingsService.Settings.SpeechVolume, playInfo.CancellationTokenSource.Token);
                Logger.LogDebug("结束播放 {}", playInfo.FilePath);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "无法播放语音。");
            }
        }
        
        _currentPlayInfo = null;
        IsPlaying = false;
    }
}