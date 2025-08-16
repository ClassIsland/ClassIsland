using System;
using System.Buffers.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums;
using ClassIsland.Shared.Abstraction.Services;
using ClassIsland.Shared.Models.Management;
using ClassIsland.Shared.Protobuf.Client;
using ClassIsland.Shared.Protobuf.Enum;
using ClassIsland.Shared.Protobuf.Server;
using ClassIsland.Shared.Protobuf.Service;
using ClassIsland.Helpers;
using ClassIsland.Services.Logging;
using ClassIsland.Shared;
using ClassIsland.Shared.Protobuf.Command;
using CsesSharp.Models;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Bcpg.OpenPgp;
using PgpCore;
using Timer = System.Timers.Timer;

namespace ClassIsland.Services.Management;

public class ManagementServerConnection : IManagementServerConnection
{
    // Cyrene_MSP, aka. "CMSP" or "Cyrene Management Server Protocol" 
    private const string ProtocolName = "Cyrene_MSP";
    
    private const string ProtocolVersion = "2.0.0.0";

    private static string ServerPublicKeyPath =>
        Path.Combine(ManagementService.ManagementConfigureFolderPath, "ServerKey.asc");

    private string? CurrentSessionId { get; set; }
    
    private ILogger<ManagementServerConnection> Logger { get; } = App.GetService<ILogger<ManagementServerConnection>>();

    private Guid ClientGuid { get; }

    private string Id { get; }
    
    private string ManifestUrl { get; }
    
    private string Host { get; }

    private GrpcChannel? Channel { get; set; }

    private DispatcherTimer CommandConnectionAliveTimer { get; } = new()
    {
        Interval = TimeSpan.FromSeconds(10)
    };
    
    private AsyncDuplexStreamingCall<ClientCommandDeliverScReq, ClientCommandDeliverScRsp>? CommandListeningCall { get;
        set;
    }

    private CancellationTokenSource CommandListeningCallCancellationTokenSource { get; set; } = new();
    
    private ManagementSettings ManagementSettings { get; }
    
    private string GetNetworkInterfaceMac() => NetworkInterface 
            .GetAllNetworkInterfaces() 
            .First(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&      // 非回环 
                        n.OperationalStatus == OperationalStatus.Up &&                  // 活动中 
                        n.GetIPProperties().UnicastAddresses.Any(ip => 
                            ip.Address.AddressFamily == AddressFamily.InterNetwork))
            .GetPhysicalAddress()
            .ToString()
            .ToUpper();
    

    private Grpc.Core.Metadata GetMetadata(bool outOfSession = false)
    {
        if (!outOfSession && CurrentSessionId == null)
        {
            throw new InvalidOperationException("当前未建立集控会话，且没有指定处于会话外连接，无法生成元数据。");
        }

        return new Grpc.Core.Metadata()
        {
            { "cuid", ClientGuid.ToString() },
            { "protocol_name", ProtocolName },
            { "protocol_version", ProtocolVersion },
            { "session", CurrentSessionId ?? "" }
        };
    }

    
    public ManagementServerConnection(ManagementSettings settings, Guid clientUid, bool lightConnect)
    {
        ClientGuid = clientUid;
        Id = settings.ClassIdentity ?? "";
        Host = settings.ManagementServer;
        ManagementSettings = settings;
        ManifestUrl = $"{Host}/api/v1/client/{clientUid}/manifest";
        CommandConnectionAliveTimer.Tick += CommandConnectionAliveTimerOnTick;
        
        Logger.LogInformation("初始化管理服务器连接。");
        if (lightConnect)
        {
            Channel = GrpcChannel.ForAddress(ManagementSettings.ManagementServerGrpc);
            return;
        }
        AppBase.Current.AppStarted += (sender, args) => InstallAuditHooks();
        // 接受命令
        CommandReceived += OnCommandReceived;
        Task.Run(ListenCommands);
    }

    private void OnCommandReceived(object? sender, ClientCommandEventArgs e)
    {
        if (e.Type != CommandTypes.GetClientConfig)
        {
            return;
        }
        var payload = GetClientConfig.Parser.ParseFrom(e.Payload);
        if (payload == null)
        {
            return;
        }
        
        Logger.LogInformation("集控请求上传配置：{} {}", payload.RequestGuid, payload.ConfigType);
        var uploadPayload = payload.ConfigType switch
        {
            ConfigTypes.AppSettings => JsonSerializer.Serialize(IAppHost.GetService<SettingsService>().Settings),
            ConfigTypes.Profile => JsonSerializer.Serialize(IAppHost.GetService<IProfileService>().Profile),
            ConfigTypes.CurrentComponent => JsonSerializer.Serialize(IAppHost.GetService<IComponentsService>()
                .CurrentComponents),
            ConfigTypes.CurrentAutomation => JsonSerializer.Serialize(IAppHost.GetService<IAutomationService>()
                .Workflows),
            ConfigTypes.Logs => JsonSerializer.Serialize(IAppHost.GetService<AppLogService>().Logs),
            ConfigTypes.PluginList => JsonSerializer.Serialize(IPluginService.LoadedPlugins
                .Where(x => x.LoadStatus == PluginLoadStatus.Loaded)
                .Select(x => x.Manifest.Id)
                .ToList()),
            _ => throw new ArgumentOutOfRangeException()
        };
        var client = new ConfigUpload.ConfigUploadClient(Channel);
        client.UploadConfig(new ConfigUploadScReq()
        {
            RequestGuidId = payload.RequestGuid,
            Payload = uploadPayload
        });
    }


    public async Task<ManagementManifest> RegisterAsync()
    {
        Logger.LogInformation("正在注册实例");
        var client = new ClientRegister.ClientRegisterClient(Channel);
        var r = await client.RegisterAsync(new ClientRegisterCsReq()
        {
            ClientUid = ClientGuid.ToString(),
            ClientId = Id,
            ClientMac = GetNetworkInterfaceMac()
        }, GetMetadata(true));
        Logger.LogTrace("ClientRegisterClient.RegisterAsync: {} {}", r.Retcode, r.Message);
        if (r.Retcode != Retcode.Registered && r.Retcode != Retcode.Success)
            throw new Exception($"无法注册实例：{r.Message}");
        await File.WriteAllTextAsync(ServerPublicKeyPath, r.ServerPublicKey);
        return await GetManifest();
    }

    private async void CommandConnectionAliveTimerOnTick(object? sender, EventArgs e)
    {
        try
        {
            if (CommandListeningCall == null)
            {
                throw new Exception("CommandListeningCall is null!");
            }
            if (CommandListeningCallCancellationTokenSource.IsCancellationRequested)
                return;
            // Logger.LogTrace("向命令流发送心跳包。");
            await CommandListeningCall.RequestStream.WriteAsync(new ClientCommandDeliverScReq()
            {
                Type = CommandTypes.Ping
            });
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "命令流与集控服务器断开。");
            await CommandListeningCallCancellationTokenSource.CancelAsync();
            CommandConnectionAliveTimer.Stop();
            CommandListeningCall = null;
            Logger.LogInformation("尝试重新连接命令流");
            _ = Task.Run(ListenCommands);
        }
    }

    private async Task<bool> BeginHandshake(CancellationToken cancellationToken)
    {
        Logger.LogInformation("准备开始和 {} 握手", ManagementSettings.ManagementServerGrpc);
        var header = GetMetadata(true);
        var mac = GetNetworkInterfaceMac();
        var handShakeClient = new Handshake.HandshakeClient(Channel);
        
        await using var publicKeyStream = File.OpenRead(ServerPublicKeyPath);
        await using var decodeStream = PgpUtilities.GetDecoderStream(publicKeyStream);
        var pgpPub = new PgpPublicKeyRing(decodeStream);
        var key = pgpPub.GetPublicKey();
        if (key == null)
        {
            throw new InvalidOperationException("服务器 GPG 密钥信息无效。");
        }
        var keyId = key.KeyId;
        var challengeToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        publicKeyStream.Position = 0;
        var pgp = new PGP(new EncryptionKeys(publicKeyStream));
        var encryptedChallengeToken = await pgp.EncryptAsync(challengeToken);
        Logger.LogTrace("BeginHandshake，挑战令牌：{}，Key ID：{}", challengeToken, keyId);
        var beginRsp = await handShakeClient.BeginHandshakeAsync(new HandshakeScBeginHandShakeReq()
        {
            ChallengeTokenEncrypted = encryptedChallengeToken,
            ClientMac = mac,
            ClientUid = ClientGuid.ToString(),
            RequestedServerKeyId = keyId
        }, header, cancellationToken: cancellationToken);

        Logger.LogTrace("BeginHandshake RESPOND!，Code：{}", beginRsp.Retcode);
        if (beginRsp.Retcode != Retcode.Success)
        {
            Logger.LogWarning("与 {} 握手失败（{}）：{}", ManagementSettings.ManagementServerGrpc, beginRsp.Retcode,
                beginRsp.Message);
            // 服务器其他异常应直接抛出
            throw new InvalidOperationException($"与 {ManagementSettings.ManagementServerGrpc} 握手失败（{beginRsp.Retcode}）：{beginRsp.Message}");
        }
        var acceptedServer = beginRsp.ChallengeTokenDecrypted == challengeToken;
        var completeRsp = await handShakeClient.CompleteHandshakeAsync(new HandshakeScCompleteHandshakeReq()
        {
            Accepted = acceptedServer
        }, header, cancellationToken: cancellationToken);
        if (!acceptedServer)
        {
            Logger.LogWarning("与 {} 握手失败：服务器密钥验证失败", ManagementSettings.ManagementServerGrpc);
            // 不信任的服务器，不再尝试握手。
            return false;
        }
        CurrentSessionId = completeRsp.SessionId;
        Logger.LogWarning("与 {} 握手成功，SessionId：{}", ManagementSettings.ManagementServerGrpc, completeRsp.SessionId);
        return true;
    }
    
    private async Task ListenCommands()
    {
        if (CommandListeningCall != null)
        {
            Logger.LogWarning("已连接到命令流，无需重复连接");
            return;
        }
        try
        {
            Logger.LogInformation("正在连接到命令流");
            Channel = GrpcChannel.ForAddress(ManagementSettings.ManagementServerGrpc);
            var handshakeState = await BeginHandshake(CommandListeningCallCancellationTokenSource.Token);
            if (!handshakeState)
            {
                Channel = null;
                Logger.LogInformation("由于对方服务器不信任，不再尝试与服务器握手和进一步连接。");
                return;
            }
            
            var client = new ClientCommandDeliver.ClientCommandDeliverClient(Channel);
            var call = client.ListenCommand(GetMetadata());
            CommandListeningCallCancellationTokenSource = new CancellationTokenSource();
            CommandListeningCall = call;
            // await call.RequestStream.WriteAsync(new ClientCommandDeliverScReq());
            CommandConnectionAliveTimer.Start();
            while (!CommandListeningCallCancellationTokenSource.IsCancellationRequested)
            {
                await call.ResponseStream.MoveNext(CommandListeningCallCancellationTokenSource.Token);
                var r = call.ResponseStream.Current;
                if (r == null)
                    continue;
                if (r.Type == CommandTypes.Pong)
                {
                    continue;
                }

                if (r.RetCode != Retcode.Success)
                {
                    Logger.LogWarning("接受指令时未返回成功代码：{}", r.RetCode);
                    continue;
                }
                Logger.LogInformation("接受指令：[{}] {}", r.Type, r.Payload);
                try
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        CommandReceived?.Invoke(this, new ClientCommandEventArgs()
                        {
                            Type = r.Type,
                            Payload = r.Payload
                        });
                    });
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "在处理事件时出现异常");
                }
            }
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
                return;
            Channel = null;
            CurrentSessionId = null;
            Logger.LogError(ex, "无法连接到集控服务器命令流，将在30秒后重试。");
            CommandConnectionAliveTimer.Stop();
            CommandListeningCall = null;
            var timer = new Timer()
            {
                Interval = 30000,
                AutoReset = false
            };
            timer.Elapsed += (sender, args) => Task.Run(ListenCommands);
            timer.Start();
        }
    }

    private void InstallAuditHooks()
    {
        
    }

    internal void LogAuditEvent(AuditEvents eventType, IBufferMessage payload)
    {
        _ = Task.Run(() =>
        {
            try
            {
                new Audit.AuditClient(Channel).LogEvent(new AuditScReq()
                {
                    Event = eventType,
                    Payload = payload.ToByteString(),
                    TimestampUtc = (long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds
                }, GetMetadata());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "无法上传审计日志 {}", eventType);
            }
        });
    }
    
    public async Task<ManagementManifest> GetManifest()
    {
        return await WebRequestHelper.GetJson<ManagementManifest>(new Uri(ManifestUrl));
    }

    private Uri DecorateUrl(string url)
    {
        var uri = url.Replace("{cuid}", ClientGuid.ToString())
            .Replace("{id}", Id)
            .Replace("{host}", Host);
        Logger.LogTrace("拼接url模板：{} -> {} ", url, uri);
        return new Uri(uri);
    }

    public async Task<T> GetJsonAsync<T>(string url)
    {
        var decorateUrl = DecorateUrl(url);
        Logger.LogInformation("发起json请求：{}", decorateUrl);
        return await WebRequestHelper.GetJson<T>(decorateUrl);
    }

    public async Task<T> SaveJsonAsync<T>(string url, string path)
    {
        var decorateUrl = DecorateUrl(url);
        Logger.LogInformation("保存json请求：{} {}", decorateUrl, path);
        return await WebRequestHelper.SaveJson<T>(decorateUrl, path);
    }

    public event EventHandler<ClientCommandEventArgs>? CommandReceived;
}