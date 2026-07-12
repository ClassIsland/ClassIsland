using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <inheritdoc />
public class AppLifetimeServiceStub : IAppLifetimeService
{
    /// <inheritdoc />
    public void Shutdown()
    {
        
    }

    /// <param name="parameters"></param>
    /// <param name="restartToLauncher"></param>
    /// <inheritdoc />
    public void Restart(string[] parameters, bool restartToLauncher)
    {
        
    }
}