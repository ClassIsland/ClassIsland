using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstraction.Services;
using ClassIsland.Core.Models.Management;
using ClassIsland.Core.Protobuf.Management;
using ClassIsland.Helpers;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.Management;

public class ManagementServerConnection : IManagementServerConnection
{
    private ILogger<ManagementServerConnection> Logger { get; } = App.GetService<ILogger<ManagementServerConnection>>();

    private Guid ClientGuid { get; }

    private string Id { get; }
    
    private string ManifestUrl { get; }
    
    private string Host { get; }
    
    private GrpcChannel Channel { get; }
    
    private Core.Protobuf.Management.ManagementServerConnection.ManagementServerConnectionClient GrpcClient { get; }
    
    public ManagementServerConnection(ManagementSettings settings, Guid clientUid, bool lightConnect)
    {
        ClientGuid = clientUid;
        Id = settings.ClassIdentity ?? "";
        Host = settings.ManagementServer;
        ManifestUrl = $"{Host}/api/v1/client/{clientUid}/manifest";
        

        Channel = GrpcChannel.ForAddress(settings.ManagementServerGrpc);
        GrpcClient =
            new Core.Protobuf.Management.ManagementServerConnection.ManagementServerConnectionClient(Channel);
        Logger.LogInformation("初始化管理服务器连接。");
        if (lightConnect) 
            return;
        // 建立Grpc连接
        // Task.Run(ListenCommands);

    }

    public async Task<ManagementManifest> RegisterAsync()
    {
        Logger.LogInformation("正在注册实例");
        await GrpcClient.RegisterAsync(new ClientRegisterInfo()
        {
            ClientUid = ClientGuid.ToString(),
            Id = Id
        });
        return await GetManifest();
    }

    private async void ListenCommands()
    {
        var r = GrpcClient?.ListenCommand(new ClientConnectRequest()
        {
            ClientId = Id,
            ClientUid = ClientGuid.ToString()
        });
        if (r == null)
        { 
            return;
        }

        await foreach (var message in r!.ResponseStream.ReadAllAsync())
        {
            
        }
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

    public event EventHandler? CommandReceived;

}