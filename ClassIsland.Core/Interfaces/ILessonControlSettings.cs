namespace ClassIsland.Core.Interfaces;

public interface ILessonControlSettings
{
    public bool ShowExtraInfoOnTimePoint
    {
        get;
        set;
    }

    public int ExtraInfoType
    {
        get;
        set;
    }

    public bool IsCountdownEnabled
    {
        get;
        set;
    }

    public int CountdownSeconds
    {
        get;
        set;
    }
}