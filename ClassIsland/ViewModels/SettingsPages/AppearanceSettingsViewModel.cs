using System.Collections.Generic;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class AppearanceSettingsViewModel : ObservableRecipient
{
    public List<FontFamily> FontFamilies { get; } =
        [..Fonts.SystemFontFamilies, new FontFamily("/ClassIsland;component/Assets/Fonts/#HarmonyOS Sans SC")];
}