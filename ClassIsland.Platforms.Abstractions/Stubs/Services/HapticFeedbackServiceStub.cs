using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <summary>
/// 不支持触觉反馈的平台默认实现。
/// </summary>
public class HapticFeedbackServiceStub : IHapticFeedbackService
{
    internal HapticFeedbackServiceStub()
    {
    }

    /// <inheritdoc />
    public void PlayTick()
    {
    }
}
