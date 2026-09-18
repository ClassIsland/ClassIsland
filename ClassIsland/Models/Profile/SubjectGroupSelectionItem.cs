using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Profile;

public sealed class SubjectGroupSelectionItem : ObservableObject
{
    private string _name;

    public SubjectGroupSelectionItem(Guid key, string name)
    {
        Key = key;
        _name = name;
    }

    public Guid Key { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}
