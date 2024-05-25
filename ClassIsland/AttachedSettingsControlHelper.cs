using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using ClassIsland.Core;
using ClassIsland.Core.Interfaces;

namespace ClassIsland;

public class AttachedSettingsControlHelper<T> : INotifyPropertyChanged, IAttachedSettingsHelper
{
    private T? _attachedSettings;
    private AttachableSettingsObject? _attachedTarget = null;

    public AttachedSettingsControlHelper(Guid id, T defaultAttachedSettings)
    {
        AttachedSettingsGuid = id;
        DefaultAttachedSettings = defaultAttachedSettings;
        PropertyChanged += Event_OnPropertyChanged;
    }

    private void Event_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AttachedTarget)) return;
        if (AttachedTarget != null)
            AttachedSettings = AttachedTarget.GetAttachedObject(AttachedSettingsGuid, DefaultAttachedSettings);
    }

    public AttachableSettingsObject? AttachedTarget
    {
        get => _attachedTarget;
        set
        {
            if (Equals(value, _attachedTarget)) return;
            _attachedTarget = value;
            OnPropertyChanged();
        }
    }

    public Guid AttachedSettingsGuid { get; }

    public T? AttachedSettings
    {
        get => _attachedSettings;
        set
        {
            _attachedSettings = value;
            OnPropertyChanged();
        }
    }

    public T DefaultAttachedSettings { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}