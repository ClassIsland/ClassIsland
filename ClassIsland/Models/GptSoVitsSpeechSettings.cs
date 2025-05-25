using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public partial class GptSoVitsSpeechSettings : ObservableObject
{
    [ObservableProperty]
    private string _presetName = "新预设";

    [ObservableProperty]
    private bool _isInternal = false;

    [ObservableProperty]
    private string _gptSoVitsServerIp = "127.0.0.1";

    [ObservableProperty]
    private string _gptSoVitsPort = "8000";

    [ObservableProperty]
    private string _gptSoVitsVoiceName = "your_voice_name";

    [ObservableProperty]
    private string _gptSoVitsTextLang = "zh";

    [ObservableProperty]
    private string _gptSoVitsRefAudioPath = "archive_jingyuan_1.wav";

    [ObservableProperty]
    private string _gptSoVitsPromptLang = "zh";

    [ObservableProperty]
    private string _gptSoVitsPromptText = "我是「罗浮」云骑将军景元。不必拘谨，「将军」只是一时的身份，你称呼我景元便可";

    [ObservableProperty]
    private string _gptSoVitsTextSplitMethod = "cut5";

    [ObservableProperty]
    private int _gptSoVitsBatchSize = 1;
}