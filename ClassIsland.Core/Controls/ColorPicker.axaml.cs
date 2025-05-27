using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ClassIsland.Core.Controls;

/// <summary>
/// ColorPicker.xaml 的交互逻辑
/// </summary>
public partial class ColorPicker : UserControl, INotifyPropertyChanged
{
    public static readonly StyledProperty<Color> ColorProperty = AvaloniaProperty.Register<ColorPicker, Color>(
        nameof(Color));

    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public string ColorHex => $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}";

    public SolidColorBrush ColorBrush => new SolidColorBrush(Color);

    public ColorPicker()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ColorHex));
        OnPropertyChanged(nameof(ColorBrush));
        base.OnPropertyChanged(e);
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

