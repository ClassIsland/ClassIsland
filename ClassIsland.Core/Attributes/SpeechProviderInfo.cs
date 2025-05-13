namespace ClassIsland.Core.Attributes;

/// <summary>
/// 语音提供方信息
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SpeechProviderInfo(string id, string name) : Attribute
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// 设置控件类型
    /// </summary>
    public Type? SettingsControlType { get; internal set; }
}