using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <inheritdoc />
public class SystemEventsServiceStub : ISystemEventsService
{
    /// <inheritdoc />
    public event EventHandler? TimeChanged;
}