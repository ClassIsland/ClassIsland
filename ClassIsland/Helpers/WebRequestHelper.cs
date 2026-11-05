using System;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Converters;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;

using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Security;
using PhainonDistributionCenter.Shared.Models.Api.Responses;
using Sentry;

namespace ClassIsland.Helpers;

public class WebRequestHelper(Uri? baseUri = null, bool phainon = false)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new ColorHexJsonConverter()
        }
    };

    /// <summary>
    /// 单次请求的超时。上游没有设置超时，沿用 <see cref="HttpClient"/> 默认的 100 秒。
    /// </summary>
    private static readonly TimeSpan PerAttemptTimeout = TimeSpan.FromSeconds(8);

    /// <summary>
    /// 一次 <see cref="GetJson{T}"/> 调用允许消耗的总时长（含重试与退避等待）。
    /// </summary>
    /// <remarks>
    /// 这是"卡死后要十几分钟才恢复"的根因修复点：
    /// 旧实现的退避是 1→2→4→8→…→128 秒（再叠加 0~50% 抖动），
    /// 在 <c>retries = 7</c> 且单次请求 100 秒超时的情况下，
    /// 最坏耗时 = 8 × 100 s（请求）+ 254 s（退避）≈ 1054 s ≈ 17.6 分钟。
    /// 期间所有等待这个结果的界面逻辑都停在原地，表现为"卡住十几分钟后自己好了"。
    /// 现在无论调用方传多少重试次数，总耗时都不会突破这个预算。
    /// </remarks>
    private static readonly TimeSpan TotalBudget = TimeSpan.FromSeconds(30);

    private HttpClient HttpClient { get; } = new(new SentryHttpMessageHandler())
    {
        BaseAddress = baseUri,
        Timeout = PerAttemptTimeout
    };

    private const int MaxRetries = 7;

    public static WebRequestHelper Default { get; } = new();

    public async Task<T> GetJson<T>(Uri uri, int retries = 3, CancellationToken? cancellationToken = null,
        bool verifySign = false, string? publicKey = null) where T : class
    {
        var logger = IAppHost.TryGetService<ILogger<WebRequestHelper>>();
        cancellationToken ??= CancellationToken.None;
        if (retries > MaxRetries)
        {
            throw new InvalidOperationException("重试次数超过规定最大重试次数。");
        }

        logger?.LogTrace("Json GET: {}", uri);
        var stopwatch = Stopwatch.StartNew();
        var retryTime = TimeSpan.FromSeconds(1);
        Exception? innerException = null;

        for (var i = 0; i <= retries; i++)
        {
            var remaining = TotalBudget - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            try
            {
                using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Value);
                attemptCts.CancelAfter(remaining < PerAttemptTimeout ? remaining : PerAttemptTimeout);

                var data = await HttpClient.GetStringAsync(uri, attemptCts.Token);
                if (verifySign && publicKey != null)
                {
                    var signUri = new UriBuilder(uri);
                    signUri.Path += ".sig";
                    var sign = await HttpClient.GetByteArrayAsync(signUri.Uri, attemptCts.Token);
                    var valid = DetachedSignatureProcessor.VerifyDetachedSignature(data, sign, publicKey);
                    if (!valid)
                    {
                        throw new GeneralSecurityException("数据签名校验失败。");
                    }
                }

                T? r;
                if (phainon)
                {
                    var phainonResult = JsonSerializer.Deserialize<Result<T>>(data, JsonOptions);
                    phainonResult?.VerifySuccess();
                    r = phainonResult?.Content;
                }
                else
                {
                    r = JsonSerializer.Deserialize<T>(data, JsonOptions);
                }
                return r ?? throw new InvalidOperationException("Json.Deserialize returned null value.");
            }
            catch (Exception ex)
            {
                innerException = ex;
                logger?.LogWarning(ex, "Json GET 请求失败（第 {} 次重试）{}", i, uri);

                retryTime *= 2;
                var realRetryTime = retryTime + retryTime * RandomNumberGenerator.GetInt32(0, 500) / 1000;

                if (i < retries && stopwatch.Elapsed + realRetryTime < TotalBudget)
                {
                    await Task.Delay(realRetryTime, cancellationToken.Value);
                }
                else
                {
                    break;
                }
            }
        }

        throw new Exception(
            $"在 {stopwatch.Elapsed.TotalSeconds:F1} 秒内无法完成对 {uri} 的 GET 请求（重试上限 {retries} 次，" +
            $"总预算 {TotalBudget.TotalSeconds:F0} 秒）：{innerException?.Message}", innerException);
    }

    public async Task<T> SaveJson<T>(Uri uri, string path, int retries = 3, CancellationToken? cancellationToken = null,
        bool verifySign = false, string? publicKey = null) where T : class
    {
        var j = await this.GetJson<T>(uri, retries, cancellationToken, verifySign, publicKey);
        ConfigureFileHelper.SaveConfig(path, j);
        return j;
    }
}
