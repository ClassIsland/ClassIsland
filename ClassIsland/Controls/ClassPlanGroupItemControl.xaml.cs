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
using ClassIsland.Services;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls;

/// <summary>
/// ClassPlanGroupItemControl.xaml 的交互逻辑
/// </summary>
public partial class ClassPlanGroupItemControl : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(
        nameof(Item), typeof(ClassPlanGroup), typeof(ClassPlanGroupItemControl), new PropertyMetadata(new ClassPlanGroup()));

    public ClassPlanGroup Item
    {
        get { return (ClassPlanGroup)GetValue(ItemProperty); }
        set { SetValue(ItemProperty, value); }
    }

    public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
        nameof(Key), typeof(string), typeof(ClassPlanGroupItemControl), new PropertyMetadata(""));

    private bool _isRenaming = false;

    public string Key
    {
        get { return (string)GetValue(KeyProperty); }
        set { SetValue(KeyProperty, value); }
    }

    private IProfileService ProfileService { get; } = App.GetService<IProfileService>();

    public bool IsRenaming
    {
        get => _isRenaming;
        set
        {
            if (value == _isRenaming) return;
            _isRenaming = value;
            OnPropertyChanged();
        }
    }

    public ClassPlanGroupItemControl()
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

    private void MenuItemRename_OnClick(object sender, RoutedEventArgs e)
    {
        IsRenaming = true;
        MainTextBox.Focus();
        MainTextBox.SelectAll();
    }

    private void TextBox_OnLostFocus(object sender, RoutedEventArgs e)
    {
        IsRenaming = false;
    }

    private void TextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is System.Windows.Input.Key.Enter or System.Windows.Input.Key.Escape)
        {
            IsRenaming = false;
            Focus();
        }
    }

    private void MenuItemTemp_OnClick(object sender, RoutedEventArgs e)
    {
        ProfileService.SetupTempClassPlanGroup(Key);
    }
}