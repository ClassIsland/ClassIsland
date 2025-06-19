using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
namespace ClassIsland.Core.Extensions;

/// <summary>
/// 为在 .xaml.cs 文件中动态创建界面提供拓展。
/// </summary>
/// <example>
/// <code>
/// new StackPanel()
///     .AddChildren(
///         new TextBlock { Text = "Item 1" },
///         new TextBlock { Text = "Item 2" })
///     .ApplyBinding(Panel.BackgroundProperty, new Binding("BgColor"));
/// </code>
/// </example>
public static class UIBuildHelperExtensions
{
    /// <summary>
    /// 为 DependencyObject 设置绑定，支持链式调用。
    /// </summary>
    /// <typeparam name="T">依赖对象类型（需继承自 DependencyObject）</typeparam>
    /// <param name="target">目标依赖对象</param>
    /// <param name="dp">目标依赖属性</param>
    /// <param name="binding">要应用的绑定</param>
    /// <exception cref="ArgumentNullException"/>
    /// <returns>this DependencyObject 自身</returns>
    public static T ApplyBinding<T>(this T target, DependencyProperty dp, BindingBase binding)
        where T : DependencyObject
    {
        BindingOperations.SetBinding(target, dp, binding);
        return target;
    }

    /// <summary>
    /// 将多个 UIElement 添加到 Panel 的 Children 集合中，支持链式调用。
    /// </summary>
    /// <typeparam name="T">Panel</typeparam>
    /// <param name="panel">Panel 对象</param>
    /// <param name="children">所有 UIElement 对象</param>
    /// <exception cref="ArgumentNullException"/>
    /// <returns>this Panel 自身</returns>
    public static T AddChildren<T>(this T panel, params UIElement[] children) where T : Panel
    {
        foreach (var child in children)
            panel.Children.Add(child);
        return panel;
    }
}