using System;
using System.Threading.Tasks;

using ClassIsland.Shared.Abstraction.Services;
using ClassIsland.Shared.Models.Management;
using ClassIsland.Helpers;

using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.Management;

public class ServerlessConnection : IManagementServerConnection
{
    private ILogger<ServerlessConnection> Logger { get; } = App.GetService<ILogger<ServerlessConnection>>();

    private Guid ClientGuid { get; }

    private string Id { get; }

    private string ManifestUrl { get; }

    public ServerlessConnection(Guid clientUid, string id, string manifestUrl)
    {
        ManifestUrl = manifestUrl;
        ClientGuid = clientUid;
        Id = id;
        Logger.LogInformation("初始化无服务器连接。");
    }

    private Uri DecorateUrl(string url)
    {
        var uri = url.Replace("{cuid}", ClientGuid.ToString()).Replace("{id}", Id);
        Logger.LogTrace("拼接url模板：{} -> {} ", url, uri);
        return new Uri(uri);
    }

    public async Task<ManagementManifest> GetManifest()
    {
        return await GetJsonAsync<ManagementManifest>(ManifestUrl);
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