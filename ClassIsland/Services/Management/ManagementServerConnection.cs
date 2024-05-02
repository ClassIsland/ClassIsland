using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstraction.Services;
using ClassIsland.Core.Models.Management;
using ClassIsland.Core.Protobuf.Client;
using ClassIsland.Core.Protobuf.Enum;
using ClassIsland.Core.Protobuf.Service;
using ClassIsland.Helpers;
using Grpc.Core;
using Grpc.Core.Utils;
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
    
    public ManagementServerConnection(ManagementSettings settings, Guid clientUid, bool lightConnect)
    {
        ClientGuid = clientUid;
        Id = settings.ClassIdentity ?? "";
        Host = settings.ManagementServer;
        ManifestUrl = $"{Host}/api/v1/client/{clientUid}/manifest";
        
        Channel = GrpcChannel.ForAddress(settings.ManagementServerGrpc);
        
        Logger.LogInformation("初始化管理服务器连接。");
        if (lightConnect) 
            return;
        // 接受命令
        Task.Run(ListenCommands);

    }

    public async Task<ManagementManifest> RegisterAsync()
    {
        Logger.LogInformation("正在注册实例");
        var client = new ClientRegister.ClientRegisterClient(Channel);
        var r = await client.RegisterAsync(new ClientRegisterCsReq()
        {
            ClientUid = ClientGuid.ToString(),
            ClientId = Id
        });
        Logger.LogTrace("ClientRegisterClient.RegisterAsync: {} {}", r.Retcode, r.Message);
        if (r.Retcode != Retcode.Registered && r.Retcode != Retcode.Success)
            throw new Exception($"无法注册实例：{r.Message}");
        return await GetManifest();
    }

    private async Task ListenCommands()
    {
        var client = new ClientCommandDeliver.ClientCommandDeliverClient(Channel);
        var stream = client.ListenCommand(new ClientCommandDeliverScReq()
        {
            ClientUid = ClientGuid.ToString()
        });
        var header = await stream.ResponseHeadersAsync;
        await stream.ResponseStream.ForEachAsync(async r =>
        {
            Logger.LogInformation("接受指令：[{}] {}", r.Type, r.Payload);
        });
        Logger.LogInformation("指令流终止。");
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