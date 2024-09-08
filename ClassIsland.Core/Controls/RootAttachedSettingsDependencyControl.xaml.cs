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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.ProfileAnalyzing;
using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;

namespace ClassIsland.Core.Controls;

/// <summary>
/// RootAttachedSettingsDependencyControl.xaml 的交互逻辑
/// </summary>
public partial class RootAttachedSettingsDependencyControl : UserControl, INotifyPropertyChanged
{
    
    public static readonly DependencyProperty ControlInfoProperty = DependencyProperty.Register(
        nameof(ControlInfo), typeof(AttachedSettingsControlInfo), typeof(RootAttachedSettingsDependencyControl), new PropertyMetadata(default(AttachedSettingsControlInfo),
            (o, args) =>
            {
                if (o is RootAttachedSettingsDependencyControl control)
                {
                    control.Update();
                }
            }));

    public AttachedSettingsControlInfo? ControlInfo
    {
        get { return (AttachedSettingsControlInfo)GetValue(ControlInfoProperty); }
        set { SetValue(ControlInfoProperty, value); }
    }

    public static readonly DependencyProperty IsDrawerModeProperty = DependencyProperty.Register(
        nameof(IsDrawerMode), typeof(bool), typeof(RootAttachedSettingsDependencyControl), new PropertyMetadata(default(bool)));

    private ObservableCollection<AttachableObjectNode> _nodes = new();

    public bool IsDrawerMode
    {
        get { return (bool)GetValue(IsDrawerModeProperty); }
        set { SetValue(IsDrawerModeProperty, value); }
    }

    public ObservableCollection<AttachableObjectNode> Nodes
    {
        get => _nodes;
        set
        {
            if (Equals(value, _nodes)) return;
            _nodes = value;
            OnPropertyChanged();
        }
    }

    private IProfileAnalyzeService ProfileAnalyzeService { get; } = IAppHost.GetService<IProfileAnalyzeService>();

    /// <inheritdoc />
    public RootAttachedSettingsDependencyControl()
    {
        InitializeComponent();
    }

    public RootAttachedSettingsDependencyControl(AttachedSettingsControlInfo controlInfo, bool isDrawerMode=false) : this()
    {
        ControlInfo = controlInfo;
        IsDrawerMode = isDrawerMode;
    }

    private void Update()
    {
        if (ControlInfo == null)
        {
            return;
        }
        ProfileAnalyzeService.Analyze();
        Nodes = new ObservableCollection<AttachableObjectNode>(ProfileAnalyzeService.Nodes.Where(x =>
            x.Value.Object != null && x.Value.Object.AttachedObjects.TryGetValue(ControlInfo.Guid.ToString(), out var value) &&
            IAttachedSettings.GetIsEnabled(value)).OrderByDescending(x => x.Value.Target).Select(x => x.Value));
    }

    #region PropertyChanged
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
    

    #endregion

    private void ButtonRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        Update();
    }
}