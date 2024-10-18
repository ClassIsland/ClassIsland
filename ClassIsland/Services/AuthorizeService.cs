using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;

namespace ClassIsland.Services;

public class AuthorizeService : IAuthorizeService
{
    public async Task<string> SetupCredentialStringAsync(string? credentialString = null, bool requireSecureDesktop = true)
    {
        return null;
    }

    public async Task<bool> AuthorizeAsync(string credentialString)
    {
        return false;
    }
}