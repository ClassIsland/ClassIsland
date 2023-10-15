using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.AttachedSettings;

public class TestAttachedSettings : ObservableRecipient
{
    private string _foo = "ely";

    public string Foo
    {
        get => _foo;
        set
        {
            if (value == _foo) return;
            _foo = value;
            OnPropertyChanged();
        }
    }
}