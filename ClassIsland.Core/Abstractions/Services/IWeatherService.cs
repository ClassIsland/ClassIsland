using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
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
    /// 立刻查询天气
    /// </summary>
    Task QueryWeatherAsync();
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
    DataTemplate? SelectedWeatherIconTemplate { get; }

    /// <summary>
    /// 已注册的天气图标模板列表。
    /// </summary>
    public static ObservableCollection<WeatherIconTemplateRegistryInfo> RegisteredTemplates { get; } = [];
}