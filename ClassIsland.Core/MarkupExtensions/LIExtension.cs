using Avalonia.Markup.Xaml;
using ClassIsland.Core.Icons;

namespace ClassIsland.Core.MarkupExtensions;

/// <summary>
/// XAML 标记扩展，用法：<c>{ci:LucideIcon access_time_regular</c>
/// </summary>
public class LIExtension
{
    /// <summary>
    /// Lucide Icon 种类
    /// </summary>
    [ConstructorArgument(nameof(Icon))]
    public LucideIconKind Icon { get; set; }
    
    /// <inheritdoc cref="LIExtension"/>
    public LIExtension() { }
    
    /// <inheritdoc cref="LIExtension"/>
    public LIExtension(LucideIconKind icon) => Icon = icon;
    
    /// <summary>
    /// 提供值
    /// </summary>
    /// <param name="serviceProvider">Avalonia 服务提供器</param>
    /// <returns>Lucide Icon 字符串值</returns>
    public string ProvideValue(IServiceProvider serviceProvider)
    {
        return char.ConvertFromUtf32((int)Icon);
    }
}