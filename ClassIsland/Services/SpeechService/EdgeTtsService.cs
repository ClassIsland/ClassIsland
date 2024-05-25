using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ClassIsland.Core.Abstraction.Services;

using Edge_tts_sharp;
using Edge_tts_sharp.Model;

using Microsoft.Extensions.Logging;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace ClassIsland.Services.SpeechService;

public class EdgeTtsService : ISpeechService
{
    public static readonly string EdgeTtsCacheFolderPath = Path.Combine(App.AppCacheFolderPath, "EdgeTTS");

    private ILogger<EdgeTtsService> Logger { get; } = App.GetService<ILogger<EdgeTtsService>>();

    private SettingsService SettingsService { get; } = App.GetService<SettingsService>();

    private List<eVoice> Voices { get; } = Edge_tts.GetVoice();

    private Queue<EdgeTtsPlayInfo> PlayingQueue { get; } = new();

    //private AudioPlayer? CurrentPlayer { get; set; }

    private bool IsPlaying { get; set; } = false;

    private CancellationTokenSource? requestingCancellationTokenSource;

    private IWavePlayer? CurrentWavePlayer { get; set; }


    public EdgeTtsService()
    {
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
        var r = requestingCancellationTokenSource;
        requestingCancellationTokenSource = new CancellationTokenSource();
        if (r is { IsCancellationRequested: false })
        {
            CancellationTokenSource.CreateLinkedTokenSource(r.Token, requestingCancellationTokenSource.Token);
        }

        var cache = GetCachePath(text);
        Logger.LogDebug("语音缓存：{}", cache);

        Task? task = null;
        if (!File.Exists(cache))
        {
            task = Task.Run(() =>
                {
                    var completed = false;
                    var voice = Voices.Find(voice => voice.ShortName == SettingsService.Settings.EdgeTtsVoiceName);
                    var completeHandle = new CancellationTokenSource();
                    Edge_tts.Invoke(text, voice, 0, (Action<List<byte>>)(binary =>
                    {
                        if (completeHandle.IsCancellationRequested)
                            return;
                        File.WriteAllBytes(cache, binary.ToArray());
                        completed = true;
                        completeHandle.Cancel();
                    }));
                    completeHandle.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(15));
                    completeHandle.Cancel();
                },
                requestingCancellationTokenSource.Token);
        }
        
        if (requestingCancellationTokenSource.IsCancellationRequested)
            return;
        PlayingQueue.Enqueue(new EdgeTtsPlayInfo(cache, new CancellationTokenSource(), task));
        _ = ProcessPlayerList();
    }

    public void ClearSpeechQueue()
    {
        requestingCancellationTokenSource?.Cancel();
        CurrentWavePlayer?.Stop();
        CurrentWavePlayer?.Dispose();
        CurrentWavePlayer = null;
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
            var playInfo = PlayingQueue.Dequeue();
            if (playInfo.CancellationTokenSource.IsCancellationRequested)
                continue;
            if (playInfo.DownloadTask != null)
            {
                Logger.LogDebug("等待下载完成");
                await playInfo.DownloadTask;
                Logger.LogDebug("等待下载完成结束");
            }

            CurrentWavePlayer?.Dispose();
            var player = CurrentWavePlayer = new DirectSoundOut();
            try
            {
                await using var audio = new AudioFileReader(playInfo.FilePath);
                var volume = new VolumeSampleProvider(audio)
                {
                    Volume = (float)SettingsService.Settings.SpeechVolume
                };
                player.Init(volume);
                Logger.LogDebug("开始播放 {}", playInfo.FilePath);
                player.Play();
                player.PlaybackStopped += (sender, args) => playInfo.IsPlayingCompleted = true;

                await Task.Run(() =>
                {
                    while (player.PlaybackState == PlaybackState.Playing &&
                           !playInfo.CancellationTokenSource.IsCancellationRequested)
                    {
                    }
                });
                Logger.LogDebug("结束播放 {}", playInfo.FilePath);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "无法播放语音。");
            }

        }

        CurrentWavePlayer?.Dispose();
        CurrentWavePlayer = null;
        IsPlaying = false;
    }
}