using ClassIsland.Core.Enums.SettingsWindow;

namespace ClassIsland.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class SettingsPageInfo : Attribute
{
    public string Name { get; } = "";
    public string Id { get; } = "";
    public string UnSelectedIconGlyph { get; } = "\uef27";
    public string SelectedIconGlyph { get; } = "\uef26";
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

    public SettingsPageInfo(string id, string name, string unSelectedIconGlyph, string selectedIconGlyph, SettingsPageCategory category = SettingsPageCategory.External) : this(id, name, category)
    {
        UnSelectedIconGlyph = unSelectedIconGlyph;
        SelectedIconGlyph = selectedIconGlyph;
    }

    public SettingsPageInfo(string id, string name, string unSelectedIconGlyph, string selectedIconGlyph, bool hideDefault, SettingsPageCategory category = SettingsPageCategory.External) : this(id, name, unSelectedIconGlyph, selectedIconGlyph, category)
    {
        HideDefault = hideDefault;
    }
}