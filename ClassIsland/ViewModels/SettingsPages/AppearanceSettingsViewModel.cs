using System.Collections.Generic;
using System.Windows.Media;
using ClassIsland.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class AppearanceSettingsViewModel : ObservableRecipient
{
    public List<FontFamily> FontFamilies { get; } =
        AppBase.Current.IsAssetsTrimmed() ? [..Fonts.SystemFontFamilies] 
            :
        [..Fonts.SystemFontFamilies, new FontFamily("/ClassIsland;component/Assets/Fonts/#HarmonyOS Sans SC")];
}