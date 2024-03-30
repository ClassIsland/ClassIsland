using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Services;
using Edge_tts_sharp;
using Edge_tts_sharp.Model;
using Edge_tts_sharp.Utils;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.SpeechService;

public class EdgeTtsService : ISpeechService
{
    private ILogger<EdgeTtsService> Logger { get; } = App.GetService<ILogger<EdgeTtsService>>();

    private SettingsService SettingsService { get; } = App.GetService<SettingsService>();

    private List<eVoice> Voices { get; } = Edge_tts.GetVoice();

    private Queue<EdgeTtsPlayerPair> PlayingQueue { get; } = new();

    private AudioPlayer? CurrentPlayer { get; set; }

    private bool IsPlaying { get; set; } = false;

    private CancellationTokenSource? requestingCancellationTokenSource;


    public EdgeTtsService()
    {
        
        Logger.LogInformation("初始化了EdgeTTS服务。");
    }

    public async void EnqueueSpeechQueue(string text)
    {
        Logger.LogInformation("以{}朗读文本：{}", SettingsService.Settings.EdgeTtsVoiceName, text);
        var r = requestingCancellationTokenSource;
        requestingCancellationTokenSource = new CancellationTokenSource();
        if (r is { IsCancellationRequested: false })
        {
            CancellationTokenSource.CreateLinkedTokenSource(r.Token, requestingCancellationTokenSource.Token);
        }
        var player = await Task.Run(() => Edge_tts.GetPlayer(text,
            Voices.Find(voice => voice.ShortName == SettingsService.Settings.EdgeTtsVoiceName), 0,
            (float)SettingsService.Settings.SpeechVolume), requestingCancellationTokenSource.Token);
        if (requestingCancellationTokenSource.IsCancellationRequested)
            return;
        PlayingQueue.Enqueue(new EdgeTtsPlayerPair(player, new CancellationTokenSource()));
        _ = ProcessPlayerList();
    }

    public void ClearSpeechQueue()
    {
        requestingCancellationTokenSource?.Cancel();
        foreach (var pair in PlayingQueue)
        {
            pair.CancellationTokenSource.Cancel();
        }
        CurrentPlayer?.Stop();
    }

    private async Task ProcessPlayerList()
    {
        if (IsPlaying)
            return;
        IsPlaying = true;
        while (PlayingQueue.Count > 0)
        {
            var player = PlayingQueue.Dequeue();
            if (player.CancellationTokenSource.IsCancellationRequested)
                break;
            CurrentPlayer = player.Player;
            var t = player.Player.PlayAsync();
            await t.WaitAsync(player.CancellationTokenSource.Token);
        }

        CurrentPlayer = null;
        IsPlaying = false;
    }
}