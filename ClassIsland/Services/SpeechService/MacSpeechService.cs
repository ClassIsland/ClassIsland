using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.SpeechService;

[SpeechProviderInfo("classisland.speech.mac", "macOS 系统语音 (say)")]
[SupportedOSPlatform("macos")]
public class MacSpeechService : ISpeechService
{
    private ILogger<MacSpeechService> Logger { get; } = App.GetService<ILogger<MacSpeechService>>();
    private readonly Queue<string> _speechQueue = new();
    private Process? _currentProcess;
    private bool _isSpeaking = false;
    private readonly object _lock = new();

    public MacSpeechService()
    {
        Logger.LogInformation("初始化了 macOS 原生系统语音服务 (say)。");
    }

    public void EnqueueSpeechQueue(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        Logger.LogInformation("macOS 语音排队朗读：{}", text);
        lock (_lock)
        {
            _speechQueue.Enqueue(text);
        }
        _ = ProcessQueueAsync();
    }

    public void ClearSpeechQueue()
    {
        lock (_lock)
        {
            _speechQueue.Clear();
            try
            {
                if (_currentProcess != null && !_currentProcess.HasExited)
                {
                    _currentProcess.Kill();
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug("终止当前语音进程异常：{}", ex.Message);
            }
            _currentProcess = null;
        }
    }

    private async Task ProcessQueueAsync()
    {
        lock (_lock)
        {
            if (_isSpeaking)
                return;
            _isSpeaking = true;
        }

        try
        {
            while (true)
            {
                string? text = null;
                lock (_lock)
                {
                    if (_speechQueue.Count == 0)
                        break;
                    text = _speechQueue.Dequeue();
                }

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/say",
                        Arguments = $"\"{text.Replace("\"", "\\\"")}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = _currentProcess = Process.Start(psi);
                    if (proc != null)
                    {
                        await proc.WaitForExitAsync();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "macOS say 朗读文本失败：{}", text);
                }
                finally
                {
                    _currentProcess = null;
                }
            }
        }
        finally
        {
            lock (_lock)
            {
                _isSpeaking = false;
            }
        }
    }
}
