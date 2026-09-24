#if DEBUG
using System.Linq;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.Input;

namespace ClassIsland.ViewModels;

public partial class ProfileSettingsViewModel
{
    [RelayCommand]
    private void AddProfileMigration()
    {
        ProfileService.Profile.Migrations = [.. ProfileService.Profile.Migrations, new ProfileMigration()];
    }

    [RelayCommand(CanExecute = nameof(CanRemoveProfileMigration))]
    private void RemoveProfileMigration(ProfileMigration? migration)
    {
        if (migration == null)
        {
            return;
        }

        ProfileService.Profile.Migrations = ProfileService.Profile.Migrations
            .Where(item => item != migration).ToList();
    }

    private bool CanRemoveProfileMigration(ProfileMigration? migration) => migration != null;
}
#endif
