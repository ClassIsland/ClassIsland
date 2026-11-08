using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.MainViewParts;

internal sealed class ProfileManagementViewModel(SettingsService settingsService) : ObservableObject
{
    public ObservableCollection<ProfileEntry> Profiles { get; } = [];

    public void RefreshProfiles()
    {
        var selected = settingsService.Settings.SelectedProfile;
        Profiles.Clear();
        foreach (var path in Directory.GetFiles(ProfileService.ProfilePath, "*.json").OrderBy(x => x))
        {
            var name = Path.GetFileName(path);
            Profiles.Add(new ProfileEntry(name,
                string.Equals(name, selected, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
