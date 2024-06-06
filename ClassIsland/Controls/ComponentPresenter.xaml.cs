using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Components;

namespace ClassIsland.Controls;

/// <summary>
/// ComponentPresenter.xaml 的交互逻辑
/// </summary>
public partial class ComponentPresenter : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
        nameof(Settings), typeof(ComponentSettings), typeof(ComponentPresenter), new PropertyMetadata(default(ComponentSettings), PropertyChangedCallback));

    public static readonly DependencyProperty IsPresentingSettingsProperty = DependencyProperty.Register(
        nameof(IsPresentingSettings), typeof(bool), typeof(ComponentPresenter), new PropertyMetadata(false));

    public bool IsPresentingSettings
    {
        get { return (bool)GetValue(IsPresentingSettingsProperty); }
        set { SetValue(IsPresentingSettingsProperty, value); }
    }

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ComponentPresenter p)
        {
            p.UpdateContent();
        }
    }

    private object? _presentingContent;

    public ComponentSettings? Settings
    {
        get { return (ComponentSettings)GetValue(SettingsProperty); }
        set { SetValue(SettingsProperty, value); }
    }

    private void UpdateContent()
    {
        if (Settings == null)
            return;
        var content = App.GetService<IComponentsService>().GetComponent(Settings, IsPresentingSettings);
        // 理论上展示的内容的数据上下文应为MainWindow，这里不便用前端xaml绑定，故在后台设置。
        if (content != null)
        {
            content.DataContext = Window.GetWindow(this);
        }

        PresentingContent = content;
    }

    public object? PresentingContent
    {
        get => _presentingContent;
        set
        {
            if (Equals(value, _presentingContent)) return;
            _presentingContent = value;
            OnPropertyChanged();
        }
    }

    public ComponentPresenter()
    {
        InitializeComponent();
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
}