using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace ClassIsland.Controls;

/// <summary>
/// TypingControl.xaml 的交互逻辑
/// </summary>
public partial class TypingControl : UserControl
{
    private string _displayingText;

    public static readonly DirectProperty<TypingControl, string> DisplayingTextProperty = AvaloniaProperty.RegisterDirect<TypingControl, string>(
        nameof(DisplayingText), o => o.DisplayingText, (o, v) => o.DisplayingText = v);

    public string DisplayingText
    {
        get => _displayingText;
        set => SetAndRaise(DisplayingTextProperty, ref _displayingText, value);
    }

    private string _text;

    public static readonly DirectProperty<TypingControl, string> TextProperty = AvaloniaProperty.RegisterDirect<TypingControl, string>(
        nameof(Text), o => o.Text, (o, v) => o.Text = v);

    public string Text
    {
        get => _text;
        set => SetAndRaise(TextProperty, ref _text, value);
    }

    private bool _isBusy;

    public static readonly DirectProperty<TypingControl, bool> IsBusyProperty = AvaloniaProperty.RegisterDirect<TypingControl, bool>(
        nameof(IsBusy), o => o.IsBusy, (o, v) => o.IsBusy = v);

    public bool IsBusy
    {
        get => _isBusy;
        set => SetAndRaise(IsBusyProperty, ref _isBusy, value);
    }

    private int _displayingIndex;

    public static readonly DirectProperty<TypingControl, int> DisplayingIndexProperty = AvaloniaProperty.RegisterDirect<TypingControl, int>(
        nameof(DisplayingIndex), o => o.DisplayingIndex, (o, v) => o.DisplayingIndex = v);

    public int DisplayingIndex
    {
        get => _displayingIndex;
        set => SetAndRaise(DisplayingIndexProperty, ref _displayingIndex, value);
    }

    private bool _isTextVisible;

    public static readonly DirectProperty<TypingControl, bool> IsTextVisibleProperty = AvaloniaProperty.RegisterDirect<TypingControl, bool>(
        nameof(IsTextVisible), o => o.IsTextVisible, (o, v) => o.IsTextVisible = v);

    public bool IsTextVisible
    {
        get => _isTextVisible;
        set => SetAndRaise(IsTextVisibleProperty, ref _isTextVisible, value);
    }

    private bool _isFirstUpdate = true;

    public TypingControl()
    {
        this.GetObservable(TextProperty).Skip(1).Subscribe(_ => UpdateText());
        InitializeComponent();
    }

    private async void UpdateText()
    {
        // TODO: 动画
        IsBusy = true;
        if (!_isFirstUpdate)
        {
            DisplayingText = "";
            await Task.Delay(TimeSpan.FromMilliseconds(150));
            for (int i = 0; i < Text.Length; i++)
            {
                DisplayingText = Text[..i] + ((i / 10) % 2 == 0 ? "_" : "");
                await Task.Delay(TimeSpan.FromMilliseconds(40));
            }
        }

        _isFirstUpdate = false;

        DisplayingText = Text;  
        IsBusy = false;
    }
}
