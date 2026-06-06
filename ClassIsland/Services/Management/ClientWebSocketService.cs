using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared.Models.Management;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.Management;

/// <summary>
/// 客户端 WebSocket 服务 - 连接集控服务器实时推送通道
/// </summary>
public class ClientWebSocketService : IDisposable
{
    private readonly ILogger<ClientWebSocketService> _logger;
    private readonly ManagementSettings _settings;
    private readonly string _clientUid;
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private bool _disposed;

    private const int ReconnectMinSeconds = 2;
    private const int ReconnectMaxSeconds = 30;
    private const int PingIntervalSeconds = 25;

    public event EventHandler<ExecuteCommandEventArgs>? CommandReceived;
    public event EventHandler? Connected;
    public event EventHandler? Disconnected;

    public bool IsConnected => _socket?.State == WebSocketState.Open;

    public ClientWebSocketService(ILogger<ClientWebSocketService> logger, ManagementSettings settings, string clientUid)
    {
        _logger = logger;
        _settings = settings;
        _clientUid = clientUid;
    }

    /// <summary>
    /// 启动 WebSocket 连接
    /// </summary>
    public async Task StartAsync(string sessionId, CancellationToken ct = default)
    {
        if (_socket != null)
        {
            _logger.LogWarning("WebSocket 已存在，先停止");
            await StopAsync();
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ = ConnectWithRetryAsync(sessionId, _cts.Token);
    }

    /// <summary>
    /// 停止 WebSocket 连接
    /// </summary>
    public async Task StopAsync()
    {
        _cts?.Cancel();
        if (_socket is { State: WebSocketState.Open })
        {
            try
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stopping", CancellationToken.None);
            }
            catch { }
        }
        _socket?.Dispose();
        _socket = null;
    }

    private async Task ConnectWithRetryAsync(string sessionId, CancellationToken ct)
    {
        var retrySeconds = ReconnectMinSeconds;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var wsUrl = BuildWebSocketUrl(sessionId);
                _logger.LogInformation("正在连接 WebSocket: {Url}", wsUrl);

                _socket = new ClientWebSocket();
                _socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(PingIntervalSeconds);
                // 绕过 SSL 证书验证（开发环境自签名证书）
                _socket.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;

                await _socket.ConnectAsync(new Uri(wsUrl), ct);
                _logger.LogInformation("WebSocket 已连接");
                Connected?.Invoke(this, EventArgs.Empty);
                retrySeconds = ReconnectMinSeconds;

                await ReceiveLoopAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WebSocket 连接失败，{Seconds} 秒后重试", retrySeconds);
            }

            Disconnected?.Invoke(this, EventArgs.Empty);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(retrySeconds), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            retrySeconds = Math.Min(retrySeconds * 2, ReconnectMaxSeconds);
        }
    }

    private string BuildWebSocketUrl(string sessionId)
    {
        var httpUrl = _settings.ManagementServer.TrimEnd('/');
        var wsBase = httpUrl.Replace("http://", "ws://").Replace("https://", "wss://");
        return $"{wsBase}/ws/client?cuid={_clientUid}&session={sessionId}";
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[8192];
        while (_socket is { State: WebSocketState.Open } && !ct.IsCancellationRequested)
        {
            var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogInformation("WebSocket 被服务端关闭");
                break;
            }
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await HandleMessageAsync(message);
            }
        }
    }

    private async Task HandleMessageAsync(string message)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString();

            switch (type)
            {
                case "Ping":
                    await SendAsync(new { type = "Pong" });
                    break;

                case "ExecuteCommand":
                    var payload = root.GetProperty("payload");
                    var commandId = payload.GetProperty("commandId").GetInt64();
                    var command = payload.GetProperty("command").GetString() ?? "";
                    var shell = payload.GetProperty("shell").GetInt32();
                    var timeout = payload.GetProperty("timeoutSeconds").GetInt32();

                    _logger.LogInformation("收到远程命令 #{Id}: {Command}", commandId, command);
                    CommandReceived?.Invoke(this, new ExecuteCommandEventArgs
                    {
                        CommandId = commandId,
                        Command = command,
                        Shell = shell,
                        TimeoutSeconds = timeout
                    });
                    break;

                case "PolicyUpdated":
                    _logger.LogInformation("收到策略更新通知");
                    break;

                case "DataUpdated":
                    _logger.LogInformation("收到数据更新通知");
                    break;

                default:
                    _logger.LogDebug("未知消息类型: {Type}", type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "处理 WebSocket 消息失败");
        }
    }

    private async Task SendAsync(object message)
    {
        if (_socket is not { State: WebSocketState.Open }) return;
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    /// <summary>
    /// 执行命令并回传结果
    /// </summary>
    public async Task ExecuteAndReportAsync(long commandId, string command, int shell, int timeoutSeconds)
    {
        var exitCode = -1;
        var stdout = "";
        var stderr = "";

        try
        {
            var (code, outStr, errStr) = await RunCommandAsync(command, shell, timeoutSeconds);
            exitCode = code;
            stdout = outStr;
            stderr = errStr;
        }
        catch (Exception ex)
        {
            stderr = $"执行异常: {ex.Message}";
        }

        // 回传结果到服务端
        try
        {
            var httpUrl = $"{_settings.ManagementServer}/api/v1/commands/{commandId}/result";
            var body = JsonSerializer.Serialize(new { exitCode, stdout, stderr });
            using var client = CreateHttpClient();
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            await client.PostAsync(httpUrl, content);
            _logger.LogInformation("命令 #{Id} 结果已回传，退出码: {Code}", commandId, exitCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "回传命令结果失败");
        }
    }

    private async Task<(int exitCode, string stdout, string stderr)> RunCommandAsync(string command, int shell, int timeoutSeconds)
    {
        var fileName = shell == 1 ? "pwsh" : "cmd";
        var args = shell == 1 ? $"-NoProfile -Command \"{command}\"" : $"/c {command}";

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);

        var exited = await Task.Run(() => process.WaitForExit(timeoutSeconds * 1000));
        if (!exited)
        {
            process.Kill();
            return (-1, "", "命令执行超时");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return (process.ExitCode, stdout, stderr);
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            UseProxy = false,
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            }
        };
        return new HttpClient(handler);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts?.Cancel();
        _socket?.Dispose();
    }
}

public class ExecuteCommandEventArgs : EventArgs
{
    public long CommandId { get; set; }
    public string Command { get; set; } = "";
    public int Shell { get; set; }
    public int TimeoutSeconds { get; set; }
}
