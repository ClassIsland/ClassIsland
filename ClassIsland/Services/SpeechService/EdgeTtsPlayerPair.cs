using System.Threading;

using Edge_tts_sharp.Utils;

namespace ClassIsland.Services.SpeechService;

public class EdgeTtsPlayerPair(AudioPlayer player, CancellationTokenSource tokenSource)
{
    public AudioPlayer Player { get; set; } = player;

    public CancellationTokenSource CancellationTokenSource { get; set; } = tokenSource;
}