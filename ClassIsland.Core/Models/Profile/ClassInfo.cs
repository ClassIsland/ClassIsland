using System.Text.Json.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Profile;

public class ClassInfo : ObservableRecipient
{
    private string _subjectId = "";
    private int _index = 0;
    private TimeLayout _currentTimeLayout = new();

    [JsonIgnore]
    public int Index
    {
        get => _index;
        set
        {
            if (value == _index) return;
            _index = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentTimeLayoutItem));
        }
    }

    [JsonIgnore]
    public TimeLayout CurrentTimeLayout
    {
        get => _currentTimeLayout;
        set
        {
            if (Equals(value, _currentTimeLayout)) return;
            _currentTimeLayout = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentTimeLayoutItem));
        }
    }

    [JsonIgnore] public TimeLayoutItem CurrentTimeLayoutItem => (from i in CurrentTimeLayout.Layouts where i.TimeType == 0 select i).ToList()[Index];


    public string SubjectId
    {
        get => _subjectId;
        set
        {
            if (value == _subjectId) return;
            _subjectId = value;
            OnPropertyChanged();
        }
    }
}