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
}