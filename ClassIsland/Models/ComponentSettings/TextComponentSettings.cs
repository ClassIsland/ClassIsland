using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace ClassIsland.Models.ComponentSettings;

public class TextComponentSettings : ObservableRecipient
{
    private string _textContent = "";

    public string TextContent
    {
        get => _textContent;
        set
        {
            if (value == _textContent) return;
            _textContent = value;
            OnPropertyChanged();
        }
    }
}