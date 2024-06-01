namespace ClassIsland.Shared.Enums;

public enum UpdateWorkingStatus
{
    Idle,
    CheckingUpdates,
    DownloadingUpdates,
    [Obsolete]
    NetworkError,
    ExtractingUpdates
}