using ClassIsland.Core.Enums.MainView;
using ClassIsland.Core.Helpers.UI;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Attributes;

/// <summary>
/// 主视图页面的导航信息。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MainViewPageInfo(string id, string name, string iconExpression, MainViewPageCategory category) : Attribute
{
    /// <summary>
    /// 创建普通主视图页面的导航信息。
    /// </summary>
    public MainViewPageInfo(string id, string name, string iconExpression)
        : this(id, name, iconExpression, MainViewPageCategory.Page)
    {
    }

    /// <summary>页面的唯一标识。</summary>
    public string Id { get; } = id;

    /// <summary>导航项和内容区标题使用的名称。</summary>
    public string Name { get; } = name;

    /// <summary>页面所属的导航类别。</summary>
    public MainViewPageCategory Category { get; } = category;

    /// <summary>导航项使用的图标源。</summary>
    public FAIconSource? IconSource => IconExpressionHelper.TryParseOrNull(iconExpression);

    /// <summary>内容区标题是否使用全宽布局。</summary>
    public bool UseFullWidth { get; internal set; }

    /// <summary>是否隐藏内容区标题。</summary>
    public bool HidePageTitle { get; internal set; }
}
