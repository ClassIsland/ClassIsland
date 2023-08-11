using System;

namespace ClassIsland.Enums;

public enum UpdateWorkingStatus
{
    Idle,
    CheckingUpdates,
    DownloadingUpdates,
    [Obsolete]
    NetworkError,
    ExtractingUpdates
}