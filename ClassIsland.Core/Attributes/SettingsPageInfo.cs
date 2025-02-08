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

    public bool HideDefault { get; } = false;

    public SettingsPageCategory Category { get; } = SettingsPageCategory.External;
    
    public SettingsPageInfo(string id, string name, SettingsPageCategory category=SettingsPageCategory.External)
    {
        Id = id;
        Name = name;
        Category = category;
    }

    public SettingsPageInfo(string id, string name, bool hideDefault, SettingsPageCategory category = SettingsPageCategory.External) : this(id, name, category)
    {
        HideDefault = hideDefault;
    }

    public SettingsPageInfo(string id, string name, PackIconKind unSelectedIcon, PackIconKind selectedIcon, SettingsPageCategory category = SettingsPageCategory.External) : this(id, name, category)
    {
        UnSelectedPackIcon = unSelectedIcon;
        SelectedPackIcon = selectedIcon;
    }

    public SettingsPageInfo(string id, string name, PackIconKind unSelectedIcon, PackIconKind selectedIcon, bool hideDefault, SettingsPageCategory category = SettingsPageCategory.External) : this(id, name, unSelectedIcon, selectedIcon, category)
    {
        HideDefault = hideDefault;
    }

    public SettingsPageInfo(string id, string name, string unSelectedBitmapUri, string selectedBitmapUri,
        SettingsPageCategory category = SettingsPageCategory.External) : this(id, name, category)

    {
        UnSelectedBitmapUri = unSelectedBitmapUri;
        SelectedBitmapUri = selectedBitmapUri;
        UseBitmapIcon = true;
    }

    public SettingsPageInfo(string id, string name, string unSelectedBitmapUri, string selectedBitmapUri, bool hideDefault,
        SettingsPageCategory category = SettingsPageCategory.External) : this(id, name, unSelectedBitmapUri, selectedBitmapUri, category)

    {
        HideDefault = hideDefault;
    }
}