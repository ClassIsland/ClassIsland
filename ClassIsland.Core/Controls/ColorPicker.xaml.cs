using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace ClassIsland.Core.Controls;

/// <summary>
/// ColorPicker.xaml 的交互逻辑
/// </summary>
public partial class ColorPicker : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register("Color", typeof(Color), typeof(ColorPicker), new PropertyMetadata(Colors.Black));

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public string ColorHex => $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}";

    public SolidColorBrush ColorBrush => new SolidColorBrush(Color);

    public ColorPicker()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ColorHex));
        OnPropertyChanged(nameof(ColorBrush));
        base.OnPropertyChanged(e);
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        //var dialog = new ColorDialog()
        //{
        //    FullOpen = true,
        //    Color = System.Drawing.Color.FromArgb(Color.R, Color.G, Color.B)
        //};
        //if (dialog.ShowDialog() == DialogResult.OK)
        //{
        //    Color = Color.FromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B);
        //    OnPropertyChanged(nameof(ColorHex));
        //    OnPropertyChanged(nameof(ColorBrush));
        //}
        Pop.IsOpen = true;


    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Picker.Focus();
        }
    }
}
