namespace ClassIsland.Controls;

internal interface IMainWindowNotificationVisibilityHost
{
    void AcquireTopmostLock(object token);

    void ReleaseTopmostLock(object token);
}
