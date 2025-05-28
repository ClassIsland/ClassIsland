using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Avalonia;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Helpers.Native;
using ClassIsland.Shared;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;

namespace ClassIsland.Core.Controls.CommonDialog;

/// <summary>
/// CommonDialog.xaml 的交互逻辑
/// </summary>
public partial class CommonDialog : MyWindow, INotifyPropertyChanged
{
    private string _dialogContent = "";
    private Visual? _dialogIcon;
    private ObservableCollection<DialogAction> _actions = new();
    private int _executedActionIndex = -1;
    private bool _hasInput = false;
    private string _inputResult = "";

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

    public bool HasInput
    {
        get => _hasInput;
        set
        {
            if (value == _hasInput) return;
            _hasInput = value;
            OnPropertyChanged();
        }
    }

    public string InputResult
    {
        get => _inputResult;
        set
        {
            if (value == _inputResult) return;
            _inputResult = value;
            OnPropertyChanged();
        }
    }

    public CommonDialog()
    {
        DataContext = this;
        InitializeComponent();
    }

    private static bool IsEasterEggDisabled => IAppHost.TryGetService<IManagementService>()?.Policy.DisableEasterEggs == true;

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
    
    [RelayCommand]
    private void PerformAction(DialogAction action)
    {
        ExecutedActionIndex = Actions.IndexOf(action);
        Close();
    }


    public static Task<int> ShowError(string message) =>
        new CommonDialogBuilder().SetContent(message).SetIconKind(CommonDialogIconKind.Forbidden).AddConfirmAction()
            .ShowDialog();

    public static Task<int> ShowInfo(string message) =>
        new CommonDialogBuilder().SetContent(message).SetIconKind(CommonDialogIconKind.Information).AddConfirmAction()
            .ShowDialog();

    public static Task<int> ShowHint(string message) =>
        new CommonDialogBuilder().SetContent(message).SetIconKind(CommonDialogIconKind.Hint).AddConfirmAction()
            .ShowDialog();
    
}
