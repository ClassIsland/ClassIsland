namespace ClassIsland.Models;

public class ApplicationCommand
{
    public string? UpdateReplaceTarget
    {
        get;
        set;
    }
    
    public string? UpdateDeleteTarget
    {
        get;
        set;
    }

    public bool WaitMutex
    {
        get;
        set;
    } = false;

    public bool Quiet { get; set; } = false;

    public bool PrevSessionMemoryKilled { get; set; } = false;

    public bool DisableManagement { get; set; } = false;

    public string Uri { get; set; } = "";

    public string ExternalPluginPath { get; set; } = "";

    public bool EnableSentryDebug { get; set; } = false;

    public bool Verbose { get; set; } = false;

    public bool ShowOssWatermark { get; set; } = false;

    public bool Recovery { get; set; } = false;

    public bool Diagnostic { get; set; } = false;
    public bool Safe { get; set; } = false;

    public bool SkipOobe { get; set; } = false;
}