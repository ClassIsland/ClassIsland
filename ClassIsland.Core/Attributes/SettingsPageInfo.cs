using ClassIsland.Core.Enums.SettingsWindow;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class SettingsPageInfo : Attribute
{
    public string Name { get; } = "";
    public string Id { get; } = "";
    public PackIconKind UnSelectedPackIcon { get; } = PackIconKind.CogOutline;
    public PackIconKind SelectedPackIcon { get; } = PackIconKind.Cog;
    public string UnSelectedBitmapUri { get; } = "";
    public string SelectedBitmapUri { get; } = "";
    public bool UseBitmapIcon { get; } = false;

    public SettingsPageCategory Category { get; } = SettingsPageCategory.External;
    
    public SettingsPageInfo(string id, string name, SettingsPageCategory category=SettingsPageCategory.External)
    {
        Id = id;
        Name = name;
        Category = category;
    }
    
    public SettingsPageInfo(string id, string name, PackIconKind unSelectedIcon, PackIconKind selectedIcon, SettingsPageCategory category = SettingsPageCategory.External)
    {
        Id = id;
        Name = name;
        UnSelectedPackIcon = unSelectedIcon;
        SelectedPackIcon = selectedIcon;
        Category = category;
    }
    
    public SettingsPageInfo(string id, string name, string unSelectedBitmapUri, string selectedBitmapUri, SettingsPageCategory category = SettingsPageCategory.External)
    {
        Id = id;
        Name = name;
        UnSelectedBitmapUri = unSelectedBitmapUri;
        SelectedBitmapUri = selectedBitmapUri;
        UseBitmapIcon = true;
        Category = category;
    }
}