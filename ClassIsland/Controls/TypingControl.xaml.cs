using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ClassIsland.Controls;

/// <summary>
/// TypingControl.xaml 的交互逻辑
/// </summary>
public partial class TypingControl : UserControl
{
    public static readonly DependencyProperty DisplayingTextProperty = DependencyProperty.Register(
        nameof(DisplayingText), typeof(string), typeof(TypingControl), new PropertyMetadata(default(string)));

    public string DisplayingText
    {
        get { return (string)GetValue(DisplayingTextProperty); }
        set { SetValue(DisplayingTextProperty, value); }
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(TypingControl), new PropertyMetadata(default(string), (o, args) =>
        {
            if (o is TypingControl control)
            {
                control.UpdateText();
            }
        }));

    public string Text
    {
        get { return (string)GetValue(TextProperty); }
        set { SetValue(TextProperty, value); }
    }

    public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
        nameof(IsBusy), typeof(bool), typeof(TypingControl), new PropertyMetadata(default(bool)));

    public bool IsBusy
    {
        get { return (bool)GetValue(IsBusyProperty); }
        set { SetValue(IsBusyProperty, value); }
    }

    public static readonly DependencyProperty DisplayIndexProperty = DependencyProperty.Register(
        nameof(DisplayIndex), typeof(int), typeof(TypingControl), new PropertyMetadata(default(int)));

    public static readonly DependencyProperty TextVisibilityProperty = DependencyProperty.Register(
        nameof(TextVisibility), typeof(Visibility), typeof(TypingControl), new PropertyMetadata(default(Visibility)));

    public Visibility TextVisibility
    {
        get { return (Visibility)GetValue(TextVisibilityProperty); }
        set { SetValue(TextVisibilityProperty, value); }
    }

    public int DisplayIndex
    {
        get { return (int)GetValue(DisplayIndexProperty); }
        set { SetValue(DisplayIndexProperty, value); }
    }

    private bool _isFirstUpdate = true;

    public TypingControl()
    {
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