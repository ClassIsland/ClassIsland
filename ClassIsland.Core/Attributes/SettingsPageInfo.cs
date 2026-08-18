using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Helpers.UI;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class SettingsPageInfo : Attribute
{
    private const string DefaultUnSelectedIconExpression = "\uef27";
    private const string DefaultSelectedIconExpression = "\uef26";

    private readonly string _unSelectedIconExpression = DefaultUnSelectedIconExpression;
    private readonly string _selectedIconExpression = DefaultSelectedIconExpression;

    public string Name { get; } = "";
    public string Id { get; } = "";
    public FAIconSource? UnSelectedIconSource =>
        IconExpressionHelper.TryParseOrNull(_unSelectedIconExpression);
    public FAIconSource? SelectedIconSource =>
        IconExpressionHelper.TryParseOrNull(_selectedIconExpression);
    public string UnSelectedBitmapUri { get; } = "";
    public string SelectedBitmapUri { get; } = "";
    public bool UseBitmapIcon { get; } = false;

    public bool HideDefault { get; } = false;
    public bool UseFullWidth { get; internal set; } = false;

    public bool HidePageTitle { get; internal set; } = false;
    
    public string? GroupId { get; internal set; }

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

    public SettingsPageInfo(string id, string name, string unSelectedIconExpression, string selectedIconExpression, SettingsPageCategory category = SettingsPageCategory.External) : this(id, name, category)
    {
        _unSelectedIconExpression = unSelectedIconExpression;
        _selectedIconExpression = selectedIconExpression;
    }

    public SettingsPageInfo(string id, string name, string unSelectedIconExpression, string selectedIconExpression, bool hideDefault, SettingsPageCategory category = SettingsPageCategory.External) : this(id, name, unSelectedIconExpression, selectedIconExpression, category)
    {
        HideDefault = hideDefault;
    }
}
