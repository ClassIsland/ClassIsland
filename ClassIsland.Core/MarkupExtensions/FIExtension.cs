using Avalonia.Markup.Xaml;
using ClassIsland.Core.Icons;

namespace ClassIsland.Core.MarkupExtensions;

/// <summary>
/// XAML 标记扩展，用法：<c>{ci:FluentIcon access_time_regular</c>
/// </summary>
public class FIExtension
{
    /// <summary>
    /// Fluent Icon 种类
    /// </summary>
    [ConstructorArgument(nameof(Icon))]
    public FluentIconKind Icon { get; set; }
    
    /// <inheritdoc cref="FIExtension"/>
    public FIExtension() { }
    
    /// <inheritdoc cref="FIExtension"/>
    public FIExtension(FluentIconKind icon) => Icon = icon;
    
    /// <summary>
    /// 提供值
    /// </summary>
    /// <param name="serviceProvider">Avalonia 服务提供器</param>
    /// <returns>Fluent Icon 字符串值</returns>
    public string ProvideValue(IServiceProvider serviceProvider)
    {
        return char.ConvertFromUtf32((int)Icon);
    }
}