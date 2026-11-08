using System;
using ClassIsland.Core.Abstractions.Services;

namespace ClassIsland.Extensions;

internal static class ProfileServiceExtensions
{
    public static void SaveProfile(this IProfileService profileService, Action rollback)
    {
        try
        {
            profileService.SaveProfile();
        }
        catch
        {
            rollback();
            throw;
        }
    }
}
