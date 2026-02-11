using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace ClassIsland.Core.Extensions.UI;

/// <summary>
/// <see cref="Visual"/> 的扩展方法。
/// </summary>
public static class VisualsExtensions
{
    private static readonly MethodInfo? MatchMethod = typeof(Selector).GetMethod("Match", 
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    
    private static readonly MethodInfo? GetIsMatchMethod = typeof(Selector).Assembly.GetType("Avalonia.Styling.SelectorMatch")?.GetMethod("get_IsMatch", 
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    
    /// <summary>
    /// 按照选择器查找子级 <see cref="Visual"/>
    /// </summary>
    /// <param name="visual">当前 Visual</param>
    /// <param name="selector">选择器</param>
    /// <returns>查找到的 <see cref="Visual"/>。如果结果为空，则返回 null。</returns>
    public static Visual? FindDescendantBySelector(this Visual? visual, Selector selector)
    {
        if (visual == null)
        {
            return null;
        }

        return FindDescendantBySelectorCore(visual, selector);
    }
    
    private static Visual? FindDescendantBySelectorCore(Visual visual, Selector selector)
    {
        var visualChildren = visual.GetVisualChildren().ToArray();
        var visualChildrenCount = visualChildren.Length;

        for (var i = 0; i < visualChildrenCount; i++)
        {
            Visual child = visualChildren[i];

            var result = MatchMethod?.Invoke(selector, [child, null, false]);
            if (Equals(GetIsMatchMethod?.Invoke(result, []), true))
            {
                return child;
            }

            var childResult = FindDescendantBySelectorCore(child, selector);

            if (childResult is not null)
            {
                return childResult;
            }
        }

        return null;
    }
    
    
}