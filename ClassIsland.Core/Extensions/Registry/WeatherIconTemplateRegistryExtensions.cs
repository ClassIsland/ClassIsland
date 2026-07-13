using System.Windows;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Weather;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 注册天气图标模板的扩展方法。
/// </summary>
public static class WeatherIconTemplateRegistryExtensions
{
    /// <summary>
    /// 注册天气图标模板
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> 实例</param>
    /// <param name="id">天气图标模板 ID</param>
    /// <param name="name">天气图标模板名称</param>
    /// <param name="template">天气图标模板内容</param>
    /// <param name="contributorInfo">贡献者信息</param>
    /// <returns>原来的 <see cref="IServiceCollection"/> 实例</returns>
    public static IServiceCollection AddWeatherIconTemplate(this IServiceCollection services, string id, string name, IDataTemplate template, ContributorInfo? contributorInfo = null)
    {
        IWeatherService.RegisteredTemplates.Add(new WeatherIconTemplateRegistryInfo(id, name, template, contributorInfo));
        return services;
    } 
}