namespace ClassIsland.Core.Models.Weather;

/// <summary>
/// 天气查询结果。
/// </summary>
public class WeatherQueryResult
{
    /// <summary>
    /// 查询是否成功。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 坐标是否降级了精度。
    /// </summary>
    public bool IsPrecisionDegraded { get; set; }

    /// <summary>
    /// 错误消息。
    /// 在<see cref="IsSuccess"/>为false时有效。
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 降级后的精度位数。
    /// 在<see cref="IsPrecisionDegraded"/>为true时有效。
    /// </summary>
    public int DegradedPrecision { get; set; }
}
