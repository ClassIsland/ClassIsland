using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Reactive;
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
    public static readonly StyledProperty<AttachedSettingsControlInfo?> ControlInfoProperty = AvaloniaProperty.Register<RootAttachedSettingsDependencyControl, AttachedSettingsControlInfo?>(
        nameof(ControlInfo));

    public AttachedSettingsControlInfo? ControlInfo
    {
        get => GetValue(ControlInfoProperty);
        set => SetValue(ControlInfoProperty, value);
    }

    public static readonly StyledProperty<bool> IsDrawerModeProperty = AvaloniaProperty.Register<RootAttachedSettingsDependencyControl, bool>(
        nameof(IsDrawerMode));

    public bool IsDrawerMode
    {
        get => GetValue(IsDrawerModeProperty);
        set => SetValue(IsDrawerModeProperty, value);
    }

    private ObservableCollection<AttachableObjectNode> _nodes = new();

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

        this.GetObservable(ControlInfoProperty)
            .Subscribe(new AnonymousObserver<AttachedSettingsControlInfo?>(_ => Update()));
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
            x.Value.Object != null && x.Value.Object.AttachedObjects.TryGetValue(ControlInfo.Guid, out var value) &&
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
