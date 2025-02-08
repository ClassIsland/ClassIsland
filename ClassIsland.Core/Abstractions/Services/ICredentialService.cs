namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 凭据服务，用于进行访问认证。
/// </summary>
public interface IAuthorizeService
{
    /// <summary>
    /// 显示一个窗口以修改或初始化凭据字符串。可以在 credentialString 参数传入先前的凭据字符串以修改这个凭据字符串，否则会新建一个凭据字符串。
    /// </summary>
    /// <remarks>你需要自行保管好凭据字符串。</remarks>
    /// <param name="credentialString">要修改的凭据字符串，留空以新建凭据字符串。</param>
    /// <returns>设置完成的凭据字符串</returns>
    Task<string?> SetupCredentialStringAsync(string? credentialString=null);

    /// <summary>
    /// 进行访问认证。需要用户输入与原先创建凭据时相同的凭据。如果认证成功，则返回 true。
    /// </summary>
    /// <param name="credentialString">要用于认证的凭据字符串</param>
    /// <returns>是否认证成功</returns>
    Task<bool> AuthenticateAsync(string credentialString);
}