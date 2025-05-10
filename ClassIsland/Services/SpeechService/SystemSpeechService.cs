using System.Speech.Synthesis;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared.Abstraction.Services;

using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.SpeechService;

[SpeechProviderInfo("classisland.speech.system", "系统 TTS")]
public class SystemSpeechService : ISpeechService
{
    private ILogger<SystemSpeechService> Logger { get; } = App.GetService<ILogger<SystemSpeechService>>();

    private SettingsService SettingsService { get; } = App.GetService<SettingsService>();

    private SpeechSynthesizer SpeechSynthesizer { get; } = new();

    public SystemSpeechService()
    {
        SpeechSynthesizer.SetOutputToDefaultAudioDevice();
        Logger.LogInformation("初始化了系统TTS服务。");
    }

    public void EnqueueSpeechQueue(string text)
    {
        Logger.LogInformation("朗读文本：{}", text);
        SpeechSynthesizer.Volume = (int)(SettingsService.Settings.SpeechVolume * 100);
        SpeechSynthesizer.SpeakAsync(text);
    }

    public void ClearSpeechQueue()
    {
        SpeechSynthesizer.SpeakAsyncCancelAll();
    }
}