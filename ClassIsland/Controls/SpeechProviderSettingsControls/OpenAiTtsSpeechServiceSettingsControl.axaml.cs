using System.Collections.Generic;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Services;

namespace ClassIsland.Controls.SpeechProviderSettingsControls;

/// <summary>
/// OpenAiTtsSpeechServiceSettingsControl.xaml 的交互逻辑。
/// </summary>
public partial class OpenAiTtsSpeechServiceSettingsControl : SpeechProviderControlBase
{
    public SettingsService SettingsService { get; }

    public IReadOnlyList<string> Voices { get; } =
        ["alloy", "echo", "fable", "onyx", "nova", "shimmer"];

    public IReadOnlyList<string> ResponseFormats { get; } =
        ["mp3", "opus", "aac", "flac", "wav", "pcm"];

    public OpenAiTtsSpeechServiceSettingsControl(SettingsService settingsService)
    {
        SettingsService = settingsService;
        InitializeComponent();
    }
}
