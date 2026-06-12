using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
namespace ClassIsland.Models.Actions;

/// <summary>
/// "语音播报"行动设置。
/// </summary>
public class DoSpeechActionSettings : ObservableRecipient
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}