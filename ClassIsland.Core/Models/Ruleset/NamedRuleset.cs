using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Ruleset;

public class NamedRuleset : ObservableRecipient
{
    private Guid _id = Guid.NewGuid();
    private string _name = "";
    private string _description = "";
    private Ruleset _ruleset = new();
    private int _state = 0;

    public Guid Id
    {
        get => _id;
        set
        {
            if (value == _id) return;
            _id = value;
            OnPropertyChanged();
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (value == _name) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (value == _description) return;
            _description = value;
            OnPropertyChanged();
        }
    }

    public Ruleset Ruleset
    {
        get => _ruleset;
        set
        {
            if (Equals(value, _ruleset)) return;
            _ruleset = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public int State
    {
        get => _state;
        set
        {
            if (value == _state) return;
            _state = value;
            OnPropertyChanged();
        }
    }
}
