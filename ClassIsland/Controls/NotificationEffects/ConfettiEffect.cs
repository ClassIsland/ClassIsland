using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared.Interfaces.Controls;

namespace ClassIsland.Controls.NotificationEffects;

internal sealed class ConfettiEffect : Decorator, INotificationEffectControl
{
    private readonly ConfettiAnimation _animation = new();

    private ConfettiEffect()
    {
        IsHitTestVisible = false;
        Child = _animation;
    }

    internal static ConfettiEffect? Create(NotificationEffectContext _)
    {
        return IThemeService.AnimationLevel >= 1 ? new ConfettiEffect() : null;
    }

    public event EventHandler? EffectCompleted;

    public async void Play()
    {
        if (_animation.TryPlay())
        {
            await Task.Delay(ConfettiAnimation.Duration);
        }

        EffectCompleted?.Invoke(this, EventArgs.Empty);
    }
}
