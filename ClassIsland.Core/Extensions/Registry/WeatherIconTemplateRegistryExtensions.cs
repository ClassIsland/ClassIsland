using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Controls.Templates;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models.Weather;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 注册天气图标模板的扩展方法。
/// </summary>
public static class WeatherIconTemplateRegistryExtensions
{
    /// <summary>
    /// 注册天气图标模板。使用 <see cref="ContributorRegistryExtensions.WithContributorInfo"/> 附加贡献者信息。
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> 实例</param>
    /// <param name="id">天气图标模板 ID</param>
    /// <param name="name">天气图标模板名称</param>
    /// <param name="template">天气图标模板内容</param>
    /// <returns>原来的 <see cref="IServiceCollection"/> 实例</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddWeatherIconTemplate(this IServiceCollection services, string id, string name, IDataTemplate template)
    {
        var info = new WeatherIconTemplateRegistryInfo(id, name, template);
        IWeatherService.RegisteredTemplates.Add(info);

        info.ContributorInfo = ContributorInfoHelper.Setup(Assembly.GetCallingAssembly());
        return services;
    } 
}