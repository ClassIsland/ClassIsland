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
    
    public SettingsPageInfo(string id, string name)
    {
        Id = id;
        Name = name;
    }
    
    public SettingsPageInfo(string id, string name, PackIconKind unSelectedIcon, PackIconKind selectedIcon)
    {
        Id = id;
        Name = name;
        UnSelectedPackIcon = unSelectedIcon;
        SelectedPackIcon = selectedIcon;
    }
    
    public SettingsPageInfo(string id, string name, string unSelectedBitmapUri, string selectedBitmapUri)
    {
        Id = id;
        Name = name;
        UnSelectedBitmapUri = unSelectedBitmapUri;
        SelectedBitmapUri = selectedBitmapUri;
        UseBitmapIcon = true;
    }
}