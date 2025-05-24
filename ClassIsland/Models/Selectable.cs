using System.Collections.Generic;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class Selectable<T> : ObservableRecipient
{
    private bool _isSelected = false;
    private T? _value = default;

    public Selectable(T value)
    {
        Value = value;
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (value == _isSelected) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public T? Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T?>.Default.Equals(value, _value)) return;
            _value = value;
            OnPropertyChanged();
        }
    }
}