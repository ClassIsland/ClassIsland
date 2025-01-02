using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ClassIsland.Shared.Abstraction.Services;

using Microsoft.Extensions.Logging;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace ClassIsland.Services.SpeechService
{
    public class GPTSoVITSService : ISpeechService
    {
        public static readonly string GPTSoVITSCacheFolderPath = Path.Combine(App.AppCacheFolderPath, "GPTSoVITS");

        private ILogger<GPTSoVITSService> Logger { get; } = App.GetService<ILogger<GPTSoVITSService>>();

        private SettingsService SettingsService { get; } = App.GetService<SettingsService>();

        private Queue<GPTSoVITSPlayInfo> PlayingQueue { get; } = new();

        private bool IsPlaying { get; set; } = false;

        private CancellationTokenSource? requestingCancellationTokenSource;

        private IWavePlayer? CurrentWavePlayer { get; set; }

        private readonly HttpClient httpClient;

        public GPTSoVITSService()
        {
            Logger.LogInformation("初始化了 GPTSoVITS 服务。");
            httpClient = new HttpClient();
        }

        private string GetCachePath(string text)
        {
            var data = Encoding.UTF8.GetBytes(text);
            var md5 = MD5.HashData(data);
            var md5String = md5.Aggregate("", (current, t) => current + t.ToString("x2"));
            var path = Path.Combine(GPTSoVITSCacheFolderPath, SettingsService.Settings.GPTSoVITSVoiceName, $"{md5String}.wav");
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory) && directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            return path;
        }

        public void EnqueueSpeechQueue(string text)
        {
            Logger.LogInformation("以 {VoiceName} 朗读文本：{Text}", SettingsService.Settings.GPTSoVITSVoiceName, text);
            var previousCts = requestingCancellationTokenSource;
            requestingCancellationTokenSource = new CancellationTokenSource();
            if (previousCts is { IsCancellationRequested: false })
            {
                requestingCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(previousCts.Token, requestingCancellationTokenSource.Token);
            }

            var cache = GetCachePath(text);
            Logger.LogDebug("语音缓存路径：{CachePath}", cache);

            Task? task = null;
            if (!File.Exists(cache))
            {
                task = Task.Run(async () =>
                {
                    try
                    {
                        var response = await GenerateSpeechAsync(text, cache, requestingCancellationTokenSource.Token);
                        if (!response)
                        {
                            Logger.LogError("生成语音失败，文本：{Text}", text);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "生成语音时发生异常。");
                    }
                }, requestingCancellationTokenSource.Token);
            }

            if (requestingCancellationTokenSource.IsCancellationRequested)
                return;

            PlayingQueue.Enqueue(new GPTSoVITSPlayInfo(cache, new CancellationTokenSource(), task));
            _ = ProcessPlayerList();
        }

        public void ClearSpeechQueue()
        {
            requestingCancellationTokenSource?.Cancel();
            CurrentWavePlayer?.Stop();
            CurrentWavePlayer?.Dispose();
            CurrentWavePlayer = null;
            while (PlayingQueue.Count > 0)
            {
                var playInfo = PlayingQueue.Dequeue();
                playInfo.CancellationTokenSource.Cancel();
            }
            // IsPlaying = false;
        }

        private async Task<bool> GenerateSpeechAsync(string text, string filePath, CancellationToken cancellationToken)
        {
            var settings = SettingsService.Settings;
            var serverIP = settings.GPTSoVITSServerIP;
            var port = settings.GPTSoVITSPort;

            var queryParams = new Dictionary<string, string>
            {
                { "text", text },
                { "text_lang", settings.GPTSoVITSTextLang },
                { "ref_audio_path", settings.GPTSoVITSRefAudioPath },
                { "prompt_lang", settings.GPTSoVITSPromptLang },
                { "prompt_text", settings.GPTSoVITSPromptText },
                { "text_split_method", settings.GPTSoVITSTextSplitMethod },
                { "batch_size", settings.GPTSoVITSBatchSize.ToString() },
                { "media_type", "wav" },
                { "streaming_mode", "false" }
            };

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var requestUri = $"http://{serverIP}:{port}/tts?{queryString}";

            try
            {
                Logger.LogDebug("发送 TTS 请求到：{RequestUri}", requestUri);
                using var response = await httpClient.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fs, cancellationToken);
                    Logger.LogDebug("语音生成并保存到：{FilePath}", filePath);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Logger.LogError("TTS 请求失败，状态码：{StatusCode}, 内容：{ErrorContent}", response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "发送 TTS 请求时发生异常。");
                return false;
            }
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
                    Logger.LogDebug("等待语音生成完成...");
                    await playInfo.DownloadTask;
                    Logger.LogDebug("语音生成完成。");
                }

                if (!File.Exists(playInfo.FilePath))
                {
                    Logger.LogError("语音文件不存在：{FilePath}", playInfo.FilePath);
                    continue;
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
                    Logger.LogDebug("开始播放 {FilePath}", playInfo.FilePath);
                    player.Play();
                    playInfo.IsPlayingCompleted = false;

                    var playbackTcs = new TaskCompletionSource<bool>();
                    void PlaybackStoppedHandler(object? sender, StoppedEventArgs args)
                    {
                        playInfo.IsPlayingCompleted = true;
                        playbackTcs.SetResult(true);
                    }

                    player.PlaybackStopped += PlaybackStoppedHandler;

                    await playbackTcs.Task;

                    player.PlaybackStopped -= PlaybackStoppedHandler;
                    Logger.LogDebug("结束播放 {FilePath}", playInfo.FilePath);
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

    // 辅助类来存储播放信息
    public class GPTSoVITSPlayInfo
    {
        public string FilePath { get; }
        public CancellationTokenSource CancellationTokenSource { get; }
        public Task? DownloadTask { get; }
        public bool IsPlayingCompleted { get; set; }

        public GPTSoVITSPlayInfo(string filePath, CancellationTokenSource cts, Task? downloadTask)
        {
            FilePath = filePath;
            CancellationTokenSource = cts;
            DownloadTask = downloadTask;
            IsPlayingCompleted = false;
        }
    }
}
