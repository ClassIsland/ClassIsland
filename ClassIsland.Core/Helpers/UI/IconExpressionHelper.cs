using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.SimpleExpression;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Helpers.UI;

/// <summary>
/// 图标表达式辅助类
/// </summary>
public static class IconExpressionHelper
{
    /// <summary>
    /// 已经注册的图标表达式处理器
    /// </summary>
    public static IReadOnlyDictionary<string, Func<string[], IconSource?>> IconExpressionHandlers =>
        _iconExpressionHandlers;
    
    private static Dictionary<string, Func<string[], IconSource?>> _iconExpressionHandlers = [];

    /// <summary>
    /// 注册新的图标处理器
    /// </summary>
    /// <param name="name">图标处理器名称</param>
    /// <param name="handler">图标处理器</param>
    public static void RegisterHandler(string name, Func<string[], IconSource?> handler)
    {
        _iconExpressionHandlers.Add(name, handler);
    }

    /// <summary>
    /// 尝试解析图标表达式。
    /// </summary>
    /// <param name="expr">图标表达式</param>
    /// <param name="result">解析结果</param>
    /// <returns>解析是否成功</returns>
    public static bool TryParse(string expr, [NotNullWhen(true)] out IconSource? result)
    {
        result = null;
        // 对于单字符的 iconfont，兼容回退为 FluentSystemIcon
        if (expr.Length == 1)
        {
            result = new FluentIconSource(expr);
            return true;
        }
        try
        {
            var expression = SimpleExprParser.Parse(expr);
            if (!_iconExpressionHandlers.TryGetValue(expression.FunctionName, out var handler))
            {
                return false;
            }

            result = handler(expression.Arguments);
            return result != null;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    /// <summary>
    /// 解析图标表达式。
    /// </summary>
    /// <param name="expr">图标表达式</param>
    /// <returns>解析结果</returns>
    /// <exception cref="InvalidOperationException">如果解析失败，将抛出此异常。</exception>
    public static IconSource Parse(string expr)
    {
        if (!TryParse(expr, out var result))
        {
            throw new InvalidOperationException($"表达式 {expr} 解析失败");
        }
        return result;
        
    }

    /// <summary>
    /// 尝试解析图标表达式。如果解析失败，返回 null
    /// </summary>
    /// <param name="expr">图标表达式</param>
    /// <returns>解析结果</returns>
    public static IconSource? TryParseOrNull(string expr)
    {
        _ = TryParse(expr, out var result);
        return result;
    }
}