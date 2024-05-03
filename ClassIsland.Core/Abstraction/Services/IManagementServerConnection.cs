using ClassIsland.Core.Models.Management;

namespace ClassIsland.Core.Abstraction.Services;

public interface IManagementServerConnection
{
    public Task<ManagementManifest> GetManifest();

    public Task<T> GetJsonAsync<T>(string url);

    public Task<T> SaveJsonAsync<T>(string url, string path);

    public event EventHandler<ClientCommandEventArgs>? CommandReceived;
}