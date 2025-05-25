using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;

using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Security;
using Sentry;

namespace ClassIsland.Helpers;

public class WebRequestHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private static HttpClient HttpClient { get; } = new(new SentryHttpMessageHandler());

    private static readonly int MaxRetries = 7;

    public static async Task<T> GetJson<T>(Uri uri, int retries=3, CancellationToken? cancellationToken=null, bool verifySign=false, string? publicKey=null)
    {
        var logger = IAppHost.TryGetService<ILogger<WebRequestHelper>>();
        cancellationToken = cancellationToken ?? CancellationToken.None;
        if (retries > MaxRetries)
        {
            throw new ArgumentException("重试次数超过规定最大重试次数。", nameof(retries));
        }
        logger?.LogTrace("Json GET: {}", uri);
        var retryTime = TimeSpan.FromSeconds(1);
        Exception? innerException = null;
        for (var i = 0; i <= retries; i++)
        {
            try
            {
                var data = await HttpClient.GetStringAsync(uri, cancellationToken.Value);
                if (verifySign && publicKey != null)
                {
                    var signUri = new UriBuilder(uri);
                    signUri.Path += ".sig";
                    var sign = await HttpClient.GetByteArrayAsync(signUri.Uri, CancellationToken.None);
                    var valid = DetachedSignatureProcessor.VerifyDetachedSignature(data, sign, publicKey);
                    if (!valid)
                    {
                        throw new GeneralSecurityException("数据签名校验失败。");
                    }
                }
                var r = JsonSerializer.Deserialize<T>(data, JsonOptions);
                return r == null ? throw new Exception("Json.Deserialize returned null value.") : r;
            }
            catch (Exception ex)
            {
                innerException = ex;
                logger?.LogWarning(ex, "Json GET 请求失败（第 {} 次重试）{}", i, uri);
                retryTime *= 2;
                if (i < retries)
                {
                    await Task.Run(() => cancellationToken.Value.WaitHandle.WaitOne(retryTime), cancellationToken.Value);
                }
            }
        }

        throw new Exception($"在 {retries} 次重试后无法完成对 {uri} 的GET请求。", innerException);
    }

    public static async Task<T> SaveJson<T>(Uri uri, string path, int retries = 3, CancellationToken? cancellationToken = null, bool verifySign = false, string? publicKey = null)
    {
        var j = await GetJson<T>(uri, retries, cancellationToken, verifySign, publicKey);
        ConfigureFileHelper.SaveConfig(path, j);
        return j;
    }
}