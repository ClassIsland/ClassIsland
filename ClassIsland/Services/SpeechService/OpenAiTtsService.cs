using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core;
using ClassIsland.Models;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared.Abstraction.Services;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.SpeechService;

/// <summary>
/// 使用 OpenAI /v1/audio/speech 格式的语音合成服务。
/// </summary>
[SpeechProviderInfo("classisland.speech.openai", "OpenAI TTS")]
public class OpenAiTtsService : ISpeechService
{
    public static readonly string OpenAiTtsCacheFolderPath =
        Path.Combine(CommonDirectories.AppCacheFolderPath, "OpenAITTS");

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> SupportedResponseFormats =
        ["mp3", "opus", "aac", "flac", "wav", "pcm"];

    public IAudioService AudioService { get; }
    private ILogger<OpenAiTtsService> Logger { get; }
    private SettingsService SettingsService { get; }
    private Queue<OpenAiTtsPlayInfo> PlayingQueue { get; } = new();
    private Dictionary<string, InFlightSpeechGeneration> InFlightGenerations { get; } = new();
    private object QueueLock { get; } = new();

    private bool IsPlaying { get; set; }
    private OpenAiTtsPlayInfo? _currentPlayInfo;

    public OpenAiTtsService(IAudioService audioService, ILogger<OpenAiTtsService> logger,
        SettingsService settingsService)
    {
        AudioService = audioService;
        Logger = logger;
        SettingsService = settingsService;

        Logger.LogInformation("初始化了 OpenAI TTS 服务。");
    }

    public void EnqueueSpeechQueue(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var currentSettings = SettingsService.Settings.OpenAiTtsSpeechSettings;
        var settings = new OpenAiTtsSpeechSettings
        {
            BaseUrl = currentSettings.BaseUrl ?? string.Empty,
            ApiKey = currentSettings.ApiKey ?? string.Empty,
            Model = currentSettings.Model ?? string.Empty,
            Voice = currentSettings.Voice ?? string.Empty,
            ResponseFormat = currentSettings.ResponseFormat ?? string.Empty,
            Speed = currentSettings.Speed
        };
        Logger.LogInformation("以 {Voice}（{Model}）朗读文本：{Text}", settings.Voice, settings.Model, text);

        var cancellationTokenSource = new CancellationTokenSource();
        var cache = GetCachePath(text, settings);
        Logger.LogDebug("OpenAI TTS 语音缓存路径：{CachePath}", cache);

        lock (QueueLock)
        {
            InFlightSpeechGeneration? generation = null;
            if (!File.Exists(cache))
            {
                if (InFlightGenerations.TryGetValue(cache, out generation) && generation.Task.IsCompleted)
                {
                    InFlightGenerations.Remove(cache);
                    generation = null;
                }

                if (generation == null && !File.Exists(cache))
                {
                    generation = new InFlightSpeechGeneration(new CancellationTokenSource())
                    {
                        ConsumerCount = 1
                    };
                    InFlightGenerations[cache] = generation;
                    generation.Task = GenerateSpeechAsync(text, cache, settings,
                        generation.CancellationTokenSource.Token);
                    _ = generation.Task.ContinueWith(_ => OnGenerationCompleted(cache, generation),
                        CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
                else if (generation != null)
                {
                    generation.ConsumerCount++;
                }
            }

            PlayingQueue.Enqueue(new OpenAiTtsPlayInfo(cache, cancellationTokenSource, generation));
        }
        _ = ProcessPlayerList();
    }

    public void ClearSpeechQueue()
    {
        OpenAiTtsPlayInfo? currentPlayInfo;
        lock (QueueLock)
        {
            currentPlayInfo = _currentPlayInfo;
            if (currentPlayInfo != null)
            {
                currentPlayInfo.CancellationTokenSource.Cancel();
                ReleaseGenerationConsumer(currentPlayInfo);
            }
            while (PlayingQueue.Count > 0)
            {
                CancelQueuedPlayInfo(PlayingQueue.Dequeue());
            }
        }
    }

    private void CancelQueuedPlayInfo(OpenAiTtsPlayInfo playInfo)
    {
        playInfo.CancellationTokenSource.Cancel();
        playInfo.CancellationTokenSource.Dispose();
        ReleaseGenerationConsumer(playInfo);
    }

    private static void ReleaseGenerationConsumer(OpenAiTtsPlayInfo playInfo)
    {
        if (playInfo.Generation == null || playInfo.IsGenerationConsumerReleased)
        {
            return;
        }

        playInfo.IsGenerationConsumerReleased = true;
        playInfo.Generation.ConsumerCount--;
    }

    private void OnGenerationCompleted(string cache, InFlightSpeechGeneration generation)
    {
        lock (QueueLock)
        {
            if (InFlightGenerations.TryGetValue(cache, out var current) && ReferenceEquals(current, generation))
            {
                InFlightGenerations.Remove(cache);
            }
        }
        generation.CancellationTokenSource.Dispose();
    }

    private string GetCachePath(string text, OpenAiTtsSpeechSettings settings)
    {
        var responseFormat = GetResponseFormat(settings.ResponseFormat);
        var cacheKey = string.Join("\n", text, settings.BaseUrl, settings.Model, settings.Voice,
            responseFormat, settings.Speed.ToString("R", CultureInfo.InvariantCulture));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cacheKey))).ToLowerInvariant();
        var extension = responseFormat == "pcm"
            ? "wav"
            : SupportedResponseFormats.Contains(responseFormat) ? responseFormat : "invalid";
        var path = Path.Combine(OpenAiTtsCacheFolderPath, extension, $"{hash}.{extension}");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }

    private async Task<bool> GenerateSpeechAsync(string text, string filePath, OpenAiTtsSpeechSettings settings,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        if (text.Length > 4096)
        {
            Logger.LogWarning("OpenAI TTS 文本长度超过 4096 个字符，无法生成语音。");
            return false;
        }

        if (string.IsNullOrWhiteSpace(settings.BaseUrl) || string.IsNullOrWhiteSpace(settings.Model) ||
            string.IsNullOrWhiteSpace(settings.Voice))
        {
            Logger.LogWarning("OpenAI TTS 设置不完整，请检查服务地址、模型和声音设置。");
            return false;
        }

        if (settings.Speed is < 0.25 or > 4)
        {
            Logger.LogWarning("OpenAI TTS 语速必须在 0.25 到 4 之间。");
            return false;
        }

        var responseFormat = GetResponseFormat(settings.ResponseFormat);
        if (!SupportedResponseFormats.Contains(responseFormat))
        {
            Logger.LogWarning("OpenAI TTS 不支持音频格式：{ResponseFormat}", settings.ResponseFormat);
            return false;
        }

        try
        {
            var requestUri = BuildSpeechUri(settings.BaseUrl);
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());
            }

            request.Content = new StringContent(JsonSerializer.Serialize(new
            {
                model = settings.Model,
                input = text,
                voice = settings.Voice,
                response_format = responseFormat,
                speed = settings.Speed
            }, JsonOptions), Encoding.UTF8, "application/json");

            Logger.LogDebug("发送 OpenAI TTS 请求到：{RequestUri}", requestUri);
            using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Logger.LogError("OpenAI TTS 请求失败，状态码：{StatusCode}，内容：{ErrorContent}", response.StatusCode,
                    errorContent);
                return false;
            }

            var audio = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (audio.Length == 0)
            {
                Logger.LogError("OpenAI TTS 返回了空音频。");
                return false;
            }

            var temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
            try
            {
                if (responseFormat == "pcm")
                {
                    await WritePcmAsWavAsync(temporaryPath, audio, cancellationToken);
                }
                else
                {
                    await File.WriteAllBytesAsync(temporaryPath, audio, cancellationToken);
                }
                File.Move(temporaryPath, filePath, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

            Logger.LogDebug("OpenAI TTS 语音生成并保存到：{FilePath}", filePath);
            return true;
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("已取消获取 OpenAI TTS 语音：{Text}", text);
            return false;
        }
        catch (UriFormatException ex)
        {
            Logger.LogError(ex, "OpenAI TTS 服务地址无效：{BaseUrl}", settings.BaseUrl);
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "发送 OpenAI TTS 请求时发生异常。");
            return false;
        }
    }

    private static string GetResponseFormat(string? responseFormat) =>
        responseFormat?.Trim().ToLowerInvariant() ?? string.Empty;

    private static async Task WritePcmAsWavAsync(string filePath, byte[] pcmAudio,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        var dataLength = pcmAudio.Length;

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(24000);
        writer.Write(24000 * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);
        writer.Flush();
        await stream.WriteAsync(pcmAudio.AsMemory(), cancellationToken);
    }

    private static Uri BuildSpeechUri(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) ||
            !Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme is not ("http" or "https"))
        {
            throw new UriFormatException("OpenAI TTS 服务地址必须是有效的 HTTP(S) 地址。");
        }

        var path = baseUri.AbsolutePath.TrimEnd('/');
        if (!path.EndsWith("/v1/audio/speech", StringComparison.OrdinalIgnoreCase))
        {
            path = path.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
                ? $"{path}/audio/speech"
                : $"{path}/v1/audio/speech";
        }

        var builder = new UriBuilder(baseUri)
        {
            Path = path
        };
        return builder.Uri;
    }

    private async Task ProcessPlayerList()
    {
        lock (QueueLock)
        {
            if (IsPlaying)
            {
                return;
            }

            IsPlaying = true;
        }

        try
        {
            while (true)
            {
                OpenAiTtsPlayInfo playInfo;
                lock (QueueLock)
                {
                    if (PlayingQueue.Count == 0)
                    {
                        break;
                    }

                    playInfo = _currentPlayInfo = PlayingQueue.Dequeue();
                }

                try
                {
                    if (playInfo.CancellationTokenSource.IsCancellationRequested)
                    {
                        continue;
                    }

                    if (playInfo.Generation != null)
                    {
                        Logger.LogDebug("等待 OpenAI TTS 语音生成完成。");
                        if (!await playInfo.Generation.Task.WaitAsync(playInfo.CancellationTokenSource.Token))
                        {
                            Logger.LogError("OpenAI TTS 语音生成失败：{FilePath}", playInfo.FilePath);
                            continue;
                        }
                    }

                    if (!File.Exists(playInfo.FilePath))
                    {
                        Logger.LogError("OpenAI TTS 语音文件不存在：{FilePath}", playInfo.FilePath);
                        continue;
                    }

                    Logger.LogDebug("开始播放 OpenAI TTS 语音：{FilePath}", playInfo.FilePath);
                    await AudioService.PlayAudioAsync(playInfo.FilePath,
                        (float)SettingsService.Settings.SpeechVolume, playInfo.CancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "无法播放 OpenAI TTS 语音。");
                }
                finally
                {
                    lock (QueueLock)
                    {
                        _currentPlayInfo = null;
                        ReleaseGenerationConsumer(playInfo);
                    }
                    playInfo.CancellationTokenSource.Dispose();
                }
            }
        }
        finally
        {
            var shouldRestart = false;
            lock (QueueLock)
            {
                _currentPlayInfo = null;
                IsPlaying = false;
                shouldRestart = PlayingQueue.Count > 0;
            }

            if (shouldRestart)
            {
                _ = ProcessPlayerList();
            }
        }
    }
}
