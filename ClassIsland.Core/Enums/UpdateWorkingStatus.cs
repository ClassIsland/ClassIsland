namespace ClassIsland.Core.Enums;

public enum UpdateWorkingStatus
{
    Idle,
    CheckingUpdates,
    DownloadingUpdates,
    [Obsolete]
    NetworkError,
    ExtractingUpdates
}