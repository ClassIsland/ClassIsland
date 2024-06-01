namespace ClassIsland.Shared.Models.Management;

/// <summary>
/// 可版本化字符串
/// </summary>
public struct ReVersionString
{
    public ReVersionString()
    {
        
    }

    /// <summary>
    /// 字符串值
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 版本
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// 检测给定版本是否比当前字符串的版本更新
    /// </summary>
    /// <param name="version">输入版本</param>
    /// <returns>检测结果。如果当前字符串有值，且版本大于输入版本，则返回true</returns>
    public bool IsNewerAndNotNull(int version) => !(string.IsNullOrWhiteSpace(Value)) && Version > version;
}