using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class NotificationRequest : ObservableRecipient
{
    private object? _overlayContent;
    private object _maskContent = new();
    private TimeSpan _overlayDuration = TimeSpan.FromSeconds(5);
    private TimeSpan _maskDuration = TimeSpan.FromSeconds(5);

    public object? OverlayContent
    {
        get => _overlayContent;
        set
        {
            if (Equals(value, _overlayContent)) return;
            _overlayContent = value;
            OnPropertyChanged();
        }
    }

    public object MaskContent
    {
        get => _maskContent;
        set
        {
            if (Equals(value, _maskContent)) return;
            _maskContent = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan OverlayDuration
    {
        get => _overlayDuration;
        set
        {
            if (value.Equals(_overlayDuration)) return;
            _overlayDuration = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan MaskDuration
    {
        get => _maskDuration;
        set
        {
            if (value.Equals(_maskDuration)) return;
            _maskDuration = value;
            OnPropertyChanged();
        }
    }
}