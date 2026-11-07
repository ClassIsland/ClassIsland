using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Platforms.Abstraction.Stubs.Services;
using Xunit;

namespace ClassIsland.Platforms.Abstractions.Tests;

public sealed class DeviceFeedbackContractsTests
{
    [Fact]
    public void DeviceInfo_DefaultServiceReturnsUnknownValues()
    {
        Assert.IsType<DeviceInfoServiceStub>(PlatformServices.DeviceInfoService);

        PlatformServices.DeviceInfoService.GetDeviceInfo(out var name, out var vendor);

        Assert.Equal("???", name);
        Assert.Equal("???", vendor);
    }

    [Fact]
    public void ScrollBoundary_FallsBackToTickForExistingImplementations()
    {
        var service = new TickFeedbackService();

        ((IHapticFeedbackService)service).PlayScrollBoundary();

        Assert.Equal(1, service.TickCount);
    }

    [Fact]
    public void HapticFeedback_DefaultServiceIgnoresUnsupportedFeedback()
    {
        Assert.IsType<HapticFeedbackServiceStub>(PlatformServices.HapticFeedbackService);

        PlatformServices.HapticFeedbackService.PlayTick();
        PlatformServices.HapticFeedbackService.PlayScrollBoundary();
    }

    private sealed class TickFeedbackService : IHapticFeedbackService
    {
        public int TickCount { get; private set; }

        public void PlayTick() => TickCount++;
    }
}
