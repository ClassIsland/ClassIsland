using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared.Abstraction.Services;

using Microsoft.Extensions.Logging;

using PgpCore;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;

namespace ClassIsland.Services.SpeechService;

[SpeechProviderInfo("classisland.speech.gpt-sovits", "GPT-SoVITS")]
public class GptSoVitsService : ISpeechService
{
    public static readonly string GPTSoVITSCacheFolderPath = Path.Combine(CommonDirectories.AppCacheFolderPath, "GPTSoVITS");

    public IAudioService AudioService { get; }
    private ILogger<GptSoVitsService> Logger { get; }

    private SettingsService SettingsService { get; }

    private Queue<GptSoVitsPlayInfo> PlayingQueue { get; } = new();

    private bool IsPlaying { get; set; } = false;

    private CancellationTokenSource? requestingCancellationTokenSource;

    private GptSoVitsPlayInfo? _currentPlayInfo;

    private SoundPlayer? CurrentWavePlayer { get; set; }

    public GptSoVitsService(IAudioService audioService, ILogger<GptSoVitsService> logger, SettingsService settingsService)
    {
        AudioService = audioService;
        Logger = logger;
        SettingsService = settingsService;
        
        Logger.LogInformation("初始化了 GPTSoVITS 服务。");
        
    }

    private string GetCachePath(string text)
    {
        var data = Encoding.UTF8.GetBytes(text);
        var md5 = MD5.HashData(data);
        var md5String = md5.Aggregate("", (current, t) => current + t.ToString("x2"));
        var path = Path.Combine(GPTSoVITSCacheFolderPath, SettingsService.Settings.GptSoVitsSpeechSettings.GptSoVitsVoiceName, $"{md5String}.wav");
        var directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory) && directory != null)
        {
            Directory.CreateDirectory(directory);
        }

        return path;
    }

    public void EnqueueSpeechQueue(string text)
    {
        Logger.LogInformation("以 {VoiceName} 朗读文本：{Text}", SettingsService.Settings.GptSoVitsSpeechSettings.GptSoVitsVoiceName, text);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }
        var previousCts = requestingCancellationTokenSource;
        requestingCancellationTokenSource = new CancellationTokenSource();
        if (previousCts is { IsCancellationRequested: false })
        {
            requestingCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(previousCts.Token, requestingCancellationTokenSource.Token);
        }

        var cache = GetCachePath(text);
        Logger.LogDebug("语音缓存路径：{CachePath}", cache);

        Task<bool>? task = null;
        if (!File.Exists(cache))
        {
            task = GenerateSpeechAsync(text, cache, requestingCancellationTokenSource.Token);
        }

        if (requestingCancellationTokenSource.IsCancellationRequested)
            return;

        PlayingQueue.Enqueue(new GptSoVitsPlayInfo(cache, new CancellationTokenSource(), task));
        _ = ProcessPlayerList();
    }

    public void ClearSpeechQueue()
    {
        requestingCancellationTokenSource?.Cancel();
        try
        {
            CurrentWavePlayer?.Stop();
            CurrentWavePlayer?.Dispose();
            CurrentWavePlayer = null;
        }
        catch (Exception e)
        {
            // ignored
        }

        _currentPlayInfo?.CancellationTokenSource.Cancel();
        while (PlayingQueue.Count > 0)
        {
            var playInfo = PlayingQueue.Dequeue();
            playInfo.CancellationTokenSource.Cancel();
        }
        // IsPlaying = false;
    }

    private async Task<bool> GenerateSpeechAsync(string text, string filePath, CancellationToken cancellationToken)
    {
        var httpClient = new HttpClient();
        var settings = SettingsService.Settings.GptSoVitsSpeechSettings;

        if (settings.IsInternal && GptSovitsSecrets.IsSecretsFilled)
        {
            var key = new EncryptionKeys(GptSovitsSecrets.PrivateKey, GptSovitsSecrets.PrivateKeyPassPhrase);
            var pgp = new PGP(key);
            var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var signData = new
            {
                ContentSHA256 = SHA256.HashData(Encoding.UTF8.GetBytes(text)),
                Timestamp = Convert.ToInt64(ts.TotalMilliseconds)
            };
            var sign = await pgp.SignAsync(JsonSerializer.Serialize(signData));
            httpClient.DefaultRequestHeaders.Add("X-ClassIsland-ApiSignature", Convert.ToBase64String(Encoding.UTF8.GetBytes(sign)));
        }
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ClassIsland", AppBase.AppVersion));
        var serverIp = settings.GptSoVitsServerIp;
        var port = settings.GptSoVitsPort;

        var queryParams = new Dictionary<string, string>
        {
            { "text", text },
            { "text_lang", settings.GptSoVitsTextLang },
            { "ref_audio_path", settings.GptSoVitsRefAudioPath },
            { "prompt_lang", settings.GptSoVitsPromptLang },
            { "prompt_text", settings.GptSoVitsPromptText },
            { "text_split_method", settings.GptSoVitsTextSplitMethod },
            { "batch_size", settings.GptSoVitsBatchSize.ToString() },
            { "media_type", "wav" },
            { "streaming_mode", "false" }
        };

        var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        var requestUri = $"http://{serverIp}:{port}/tts?{queryString}";

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
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
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
            var playInfo = _currentPlayInfo = PlayingQueue.Dequeue();
            if (playInfo.CancellationTokenSource.IsCancellationRequested)
                continue;
            if (playInfo.DownloadTask != null)
            {
                Logger.LogDebug("等待语音生成完成...");
                var result = await playInfo.DownloadTask;
                if (!result)
                {
                    Logger.LogError("语音 {} 生成失败。", playInfo.FilePath);
                    continue;
                }
                Logger.LogDebug("语音生成完成。");
            }

            if (!File.Exists(playInfo.FilePath))
            {
                Logger.LogError("语音文件不存在：{FilePath}", playInfo.FilePath);
                continue;
            }

            CurrentWavePlayer?.Stop();
            CurrentWavePlayer?.Dispose();
            using var device = AudioService.TryInitializeDefaultPlaybackDevice();
            device?.Start();
            try
            {
                using var player = CurrentWavePlayer = new SoundPlayer(AudioService.AudioEngine, IAudioService.DefaultAudioFormat,
                    new StreamDataProvider(AudioService.AudioEngine, IAudioService.DefaultAudioFormat, File.OpenRead(playInfo.FilePath)))
                {
                    Volume = (float)SettingsService.Settings.SpeechVolume
                };
                Logger.LogDebug("开始播放 {FilePath}", playInfo.FilePath);
                device?.MasterMixer.AddComponent(player);

                var playbackTcs = new TaskCompletionSource<bool>();
                void PlaybackStoppedHandler(object? sender, EventArgs args)
                {
                    playInfo.IsPlayingCompleted = true;
                    playbackTcs.SetResult(true);
                }

                player.PlaybackEnded += PlaybackStoppedHandler;
                playInfo.CancellationTokenSource.Token.Register(() =>
                {
                    playbackTcs.SetResult(false);
                });
                player.Play();
                playInfo.IsPlayingCompleted = false;

                await playbackTcs.Task;

                player.PlaybackEnded -= PlaybackStoppedHandler;
                Logger.LogDebug("结束播放 {FilePath}", playInfo.FilePath);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "无法播放语音。");
            }
        }

        _currentPlayInfo = null;
        CurrentWavePlayer?.Stop();
        CurrentWavePlayer?.Dispose();
        CurrentWavePlayer = null;
        IsPlaying = false;
    }
}