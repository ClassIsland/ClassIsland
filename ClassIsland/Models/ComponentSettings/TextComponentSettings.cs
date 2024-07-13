using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace ClassIsland.Models.ComponentSettings;

public class TextComponentSettings : ObservableRecipient
{
    private string _textContent = "";
    private int _fontSize = 16;
    private Color _fontColor = Color.FromRgb(255, 255, 255);

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
    public int FontSize
    {
        get => _fontSize;
        set
        {
            if (value == null) return;
            if (value.Equals(_fontSize)) return;
            _fontSize = value;
            OnPropertyChanged();
        }
    }
    public Color FontColor
    {
        get => _fontColor;
        set
        {
            if (value == null) return;
            if (value.Equals(_fontColor)) return;
            _fontColor = value;
            OnPropertyChanged();
        }
    }
}