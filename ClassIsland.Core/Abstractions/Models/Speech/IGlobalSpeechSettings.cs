using System.ComponentModel;

namespace ClassIsland.Core.Abstractions.Models.Speech;

/// <summary>
/// 全局语音设置
/// </summary>
public interface IGlobalSpeechSettings : INotifyPropertyChanged
{
    /// <summary>
    /// 语音音量，范围为 0.0（静音）~1.0（最大音量）
    /// </summary>
    public double SpeechVolume { get; set; }
}