using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

/// <summary>
/// OpenAI 兼容语音合成服务设置。
/// </summary>
public partial class OpenAiTtsSpeechSettings : ObservableObject
{
    [ObservableProperty]
    private string _baseUrl = "https://api.openai.com";

    [ObservableProperty]
    private string _apiKey = "";

    [ObservableProperty]
    private string _model = "tts-1";

    [ObservableProperty]
    private string _voice = "alloy";

    [ObservableProperty]
    private string _responseFormat = "mp3";

    [ObservableProperty]
    private double _speed = 1.0;
}
