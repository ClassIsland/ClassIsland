using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ClassIsland.Models;
using static ClassIsland.NativeWindowHelper;

namespace ClassIsland.Controls;

/// <summary>
/// CommonDialog.xaml 的交互逻辑
/// </summary>
public partial class CommonDialog : MyWindow, INotifyPropertyChanged
{
    private string _dialogContent = "";
    private Visual? _dialogIcon;
    private ObservableCollection<DialogAction> _actions = new();
    private int _executedActionIndex = -1;

    public static ICommand ActionCommand { get; } = new RoutedCommand();

    public string DialogContent
    {
        get => _dialogContent;
        set
        {
            if (value == _dialogContent) return;
            _dialogContent = value;
            OnPropertyChanged();
        }
    }


    public Visual? DialogIcon
    {
        get => _dialogIcon;
        set
        {
            if (Equals(value, _dialogIcon)) return;
            _dialogIcon = value;
            OnPropertyChanged();
        }
    }


    public ObservableCollection<DialogAction> Actions
    {
        get => _actions;
        set
        {
            if (Equals(value, _actions)) return;
            _actions = value;
            OnPropertyChanged();
        }
    }

    public int ExecutedActionIndex
    {
        get => _executedActionIndex;
        set
        {
            if (value == _executedActionIndex) return;
            _executedActionIndex = value;
            OnPropertyChanged();
        }
    }

    public CommonDialog()
    {
        DataContext = this;
        InitializeComponent();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        var hWnd = new WindowInteropHelper(this).Handle;
        //NativeWindowHelper.SetWindowLong(hWnd, NativeWindowHelper.GWL_STYLE,
        //    NativeWindowHelper.GetWindowLong(hWnd, NativeWindowHelper.GWL_STYLE) & ~NativeWindowHelper.WS_SYSMENU);
        SetWindowLong(hWnd, GWL_STYLE, GetWindowLong(hWnd, GWL_STYLE) | WS_EX_DLGMODALFRAME);
        SetWindowPos(hWnd, nint.Zero, 0, 0, 0, 0, SWP_NOMOVE |
                                                    SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
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

    private void CommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ExecutedActionIndex = Actions.IndexOf((DialogAction)e.Parameter);
        Close();
    }
}