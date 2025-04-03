using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Models.Authorize;
using ClassIsland.Views;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class AuthorizeService(ILogger<AuthorizeService> logger) : IAuthorizeService
{
    private static Credential ConvertCredentialStringToModel(string credentialString)
    {
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(credentialString));
        return JsonSerializer.Deserialize<Credential>(json) ?? new Credential();
    }

    private static string ConvertCredentialModelToString(Credential credential)
    {
        var json = JsonSerializer.Serialize(credential);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public async Task<string?> SetupCredentialStringAsync(string? credentialString = null)
    {
        try
        {
            var credential = credentialString != null ? ConvertCredentialStringToModel(credentialString) : new Credential();
            var window = new AuthorizeWindow(credential, true);
            var result = window.ShowDialog();
            return result != true ? credentialString : ConvertCredentialModelToString(credential);
        }
        catch (Exception e)
        {
            logger.LogError(e, "创建认证信息时发生异常");
            CommonDialog.ShowError($"创建认证信息时发生异常：{e.Message}");
            return credentialString;
        }
    }

    public async Task<bool> AuthenticateAsync(string credentialString)
    {
        if (string.IsNullOrWhiteSpace(credentialString))
        {
            logger.LogWarning("传入了空的认证字符串，默认为认证通过。");
            return true;
        }
        try
        {
            var credential = ConvertCredentialStringToModel(credentialString);
            var window = new AuthorizeWindow(credential, false);
            var result = window.ShowDialog();
            return result == true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "认证时发生异常");
            CommonDialog.ShowError($"认证时发生异常：{e.Message}");
            return false;
        }
        return false;
    }
}