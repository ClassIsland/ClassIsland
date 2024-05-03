namespace ClassIsland.Core.Interfaces.Controls;

public interface INotificationEffectControl
{
    public void Play();

    public event EventHandler? EffectCompleted;
}