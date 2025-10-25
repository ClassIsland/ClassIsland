using System.Windows;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;

namespace ClassIsland.Core.Models.Weather;

/// <summary>
/// 代表天气图标模板注册信息。
/// </summary>
public class WeatherIconTemplateRegistryInfo
{
    /// <summary>
    /// 图标模板 ID
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// 图标模板名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 图标模板内容
    /// </summary>
    public IDataTemplate Template { get; }

    internal WeatherIconTemplateRegistryInfo(string id, string name, IDataTemplate template)
    {
        Id = id;
        Name = name;
        Template = template;
    }
}