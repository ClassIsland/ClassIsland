using ClassIsland.Shared.Models.Management;

namespace ClassIsland.Shared.Abstraction.Services;

/// <summary>
/// 集控服务器连接接口
/// </summary>
public interface IManagementServerConnection
{
    /// <summary>
    /// 获取集控清单
    /// </summary>
    /// <returns>获取到的集控清单信息</returns>
    public Task<ManagementManifest> GetManifest();

    /// <summary>
    /// 从集控服务器获取Json信息。url中的模板将被替换，关于url模板，请见<a href="https://docs.classisland.tech/zh-cn/latest/management/client-identify/#url-template">集控文档</a>。
    /// </summary>
    /// <typeparam name="T">信息类型</typeparam>
    /// <param name="url">要获取的url</param>
    /// <returns>获取到的信息</returns>
    public Task<T> GetJsonAsync<T>(string url);

    /// <summary>
    /// 从集控服务器获取Json信息，并写入到本地文件。url中的模板将被替换，关于url模板，请见<a href="https://docs.classisland.tech/zh-cn/latest/management/client-identify/#url-template">集控文档</a>。
    /// </summary>
    /// <typeparam name="T">信息类型</typeparam>
    /// <param name="url">要获取的url</param>
    /// <param name="path">要写入的文件路径</param>
    /// <returns>获取到的信息</returns>
    public Task<T> SaveJsonAsync<T>(string url, string path);

    /// <summary>
    /// 接收到服务器命令事件
    /// </summary>
    public event EventHandler<ClientCommandEventArgs>? CommandReceived;
}