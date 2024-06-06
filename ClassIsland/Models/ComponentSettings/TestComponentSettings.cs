using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.ComponentSettings;

public class TestComponentSettings : ObservableRecipient
{
    private string _testProp = "Hello!";

    public string TestProp
    {
        get => _testProp;
        set
        {
            if (value == _testProp) return;
            _testProp = value;
            OnPropertyChanged();
        }
    }
}