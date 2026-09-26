using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using ClassIsland.Core.Models.Weather;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 天气服务。
/// </summary>
public interface IWeatherService : INotifyPropertyChanged
{
    /// <summary>
    /// 天气状态列表
    /// </summary>
    List<XiaomiWeatherStatusCodeItem> WeatherStatusList { get; set; }
    /// <summary>
    /// 天气是否已经刷新
    /// </summary>
    bool IsWeatherRefreshed { get; set; }

    /// <summary>
    /// 最近一次获取到的天气信息。如果尚未获取到天气信息，则为 <see langword="null"/>。
    /// </summary>
    /// <remarks>
    /// 提供默认实现以保持源代码兼容性：使用旧版 SDK 编译、未实现此成员的现有实现
    /// （如第三方插件或测试替身）仍可正常加载，调用时将回退为 <see langword="null"/>。
    /// </remarks>
    WeatherInfo? LastWeatherInfo => null;
    /// <summary>
    /// 立刻查询天气
    /// </summary>
    Task QueryWeatherAsync();
    /// <summary>
    /// 立刻查询天气并返回结果
    /// </summary>
    /// <returns>天气查询结果</returns>
    Task<WeatherQueryResult> QueryWeatherWithResultAsync();

    /// <summary>
    /// 根据天气代码获得天气名称
    /// </summary>
    /// <param name="code">天气代码</param>
    /// <returns>对应的天气名称。如果不存在，则返回“未知”。</returns>
    string GetWeatherTextByCode(string code);
    /// <summary>
    /// 按省份和城市名搜索城市
    /// </summary>
    /// <param name="name">搜索字符串</param>
    /// <returns>匹配搜索的城市列表</returns>
    Task<List<City>> GetCitiesByName(string name);

    /// <summary>
    /// 当前天气图标模板
    /// </summary>
    IDataTemplate? SelectedWeatherIconTemplate { get; }

    /// <summary>
    /// 已注册的天气图标模板列表。
    /// </summary>
    public static ObservableCollection<WeatherIconTemplateRegistryInfo> RegisteredTemplates { get; } = [];
}