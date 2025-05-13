namespace ClassIsland.Core.Models;

/// <summary>
/// 代表一个位置坐标
/// </summary>
public record LocationCoordinate
{
    /// <summary>
    /// 经度
    /// </summary>
    public double Longitude { get; set; } = 0;

    /// <summary>
    /// 纬度
    /// </summary>
    public double Latitude { get; set; } = 0;  
}