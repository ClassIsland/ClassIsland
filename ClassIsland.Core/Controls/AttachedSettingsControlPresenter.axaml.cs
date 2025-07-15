using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Models.ProfileAnalyzing;
using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Profile;


namespace ClassIsland.Core.Controls;

/// <summary>
/// AttachedSettingsControlPresenter.xaml 的交互逻辑
/// </summary>
public partial class AttachedSettingsControlPresenter : UserControl, INotifyPropertyChanged
{
    public IProfileAnalyzeService ProfileAnalyzeService { get; }
    public IProfileService ProfileService { get; }
    public IManagementService ManagementService { get; }

    public static readonly StyledProperty<AttachedSettingsControlInfo> ControlInfoProperty = AvaloniaProperty.Register<AttachedSettingsControlPresenter, AttachedSettingsControlInfo>(
        nameof(ControlInfo));

    public AttachedSettingsControlInfo ControlInfo
    {
        get => GetValue(ControlInfoProperty);
        set => SetValue(ControlInfoProperty, value);
    }

    public static readonly StyledProperty<AttachableSettingsObject> TargetObjectProperty = AvaloniaProperty.Register<AttachedSettingsControlPresenter, AttachableSettingsObject>(
        nameof(TargetObject));

    public AttachableSettingsObject TargetObject
    {
        get => GetValue(TargetObjectProperty);
        set => SetValue(TargetObjectProperty, value);
    }

    public static readonly StyledProperty<object?> ContentObjectProperty = AvaloniaProperty.Register<AttachedSettingsControlPresenter, object?>(
        nameof(ContentObject));

    public object? ContentObject
    {
        get => GetValue(ContentObjectProperty);
        set => SetValue(ContentObjectProperty, value);
    }

    public static readonly StyledProperty<IAttachedSettings?> AssociatedAttachedSettingsProperty = AvaloniaProperty.Register<AttachedSettingsControlPresenter, IAttachedSettings?>(
        nameof(AssociatedAttachedSettings));

    public IAttachedSettings? AssociatedAttachedSettings
    {
        get => GetValue(AssociatedAttachedSettingsProperty);
        set => SetValue(AssociatedAttachedSettingsProperty, value);
    }

    public static readonly StyledProperty<Guid> ContentIdProperty = AvaloniaProperty.Register<AttachedSettingsControlPresenter, Guid>(
        nameof(ContentId));

    public Guid ContentId
    {
        get => GetValue(ContentIdProperty);
        set => SetValue(ContentIdProperty, value);
    }

    public static readonly StyledProperty<int> ContentIndexProperty = AvaloniaProperty.Register<AttachedSettingsControlPresenter, int>(
        nameof(ContentIndex));

    public int ContentIndex
    {
        get => GetValue(ContentIndexProperty);
        set => SetValue(ContentIndexProperty, value);
    }


    private ObservableCollection<AttachableObjectNode> _nextItems = new();
    private ObservableCollection<AttachableObjectNode> _previousItems = new();
    private char _dependencyItemPackIconKind = '\0';
    private string _dependencyItemTitle = "";
    private bool _isLoading = false;

    public static readonly StyledProperty<bool> IsPopupOpenedProperty = AvaloniaProperty.Register<AttachedSettingsControlPresenter, bool>(
        nameof(IsPopupOpened));

    public bool IsPopupOpened
    {
        get => GetValue(IsPopupOpenedProperty);
        set => SetValue(IsPopupOpenedProperty, value);
    }

    public static readonly StyledProperty<bool> IsDependencyModeProperty = AvaloniaProperty.Register<AttachedSettingsControlPresenter, bool>(
        nameof(IsDependencyModeProperty));

    public bool IsDependencyMode
    {
        get => GetValue(IsDependencyModeProperty);
        set => SetValue(IsDependencyModeProperty, value);
    }

    public ObservableCollection<AttachableObjectNode> NextItems
    {
        get => _nextItems;
        set
        {
            if (Equals(value, _nextItems)) return;
            _nextItems = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<AttachableObjectNode> PreviousItems
    {
        get => _previousItems;
        set
        {
            if (Equals(value, _previousItems)) return;
            _previousItems = value;
            OnPropertyChanged();
        }
    }

    public char DependencyItemPackIconKind
    {
        get => _dependencyItemPackIconKind;
        set
        {
            if (value == _dependencyItemPackIconKind) return;
            _dependencyItemPackIconKind = value;
            OnPropertyChanged();
        }
    }

    public string DependencyItemTitle
    {
        get => _dependencyItemTitle;
        set
        {
            if (value == _dependencyItemTitle) return;
            _dependencyItemTitle = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (value == _isLoading) return;
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public static readonly StyledProperty<AttachedSettingsControlState> StateProperty = AvaloniaProperty.Register<AttachedSettingsControlPresenter, AttachedSettingsControlState>(
        nameof(State));

    public AttachedSettingsControlState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public AttachedSettingsControlPresenter(IProfileAnalyzeService profileAnalyzeService, IProfileService profileService, IManagementService managementService)
    {
        ProfileAnalyzeService = profileAnalyzeService;
        ProfileService = profileService;
        ManagementService = managementService;
    }

    public AttachedSettingsControlPresenter() : this(IAppHost.GetService<IProfileAnalyzeService>(), IAppHost.GetService<IProfileService>(), IAppHost.GetService<IManagementService>())
    {
        InitializeComponent();
        UpdateContent();
    }

    private async void ButtonShowDetails_OnClick(object sender, RoutedEventArgs e)
    {
        IsPopupOpened = true;
        await AnalyzeAsync();
    }

    private async Task AnalyzeAsync()
    {
        if (ControlInfo == null || ProfileAnalyzeService == null || IsLoading)
        {
            return;
        }
        IsLoading = true;
        var contentId = ContentId;
        var contentIndex = ContentIndex;
        var guid = ControlInfo.Guid;
        await Task.Run(() =>
        {
            ProfileAnalyzeService.Analyze();
            NextItems = new ObservableCollection<AttachableObjectNode>(ProfileAnalyzeService.FindNextObjects(new AttachableObjectAddress(contentId, contentIndex),
                guid)!);
            PreviousItems = new ObservableCollection<AttachableObjectNode>(ProfileAnalyzeService.FindPreviousObjects(
                new AttachableObjectAddress(contentId, contentIndex),
                guid)!);
        });
        IsLoading = false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TargetObjectProperty || e.Property == ControlInfoProperty || e.Property == ContentIdProperty)
        {
            UpdateContent();
        }
        base.OnPropertyChanged(e);
    }

    private void UpdateContent()
    {
        if (TargetObject == null || ControlInfo == null)
        {
            return;
        }

        TargetObject.AttachedObjects.TryGetValue(ControlInfo.Guid, out var settings);
        ContentObject = AttachedSettingsControlBase.GetInstance(ControlInfo, ref settings);
        //if (ContentObject is IAttachedSettingsControlBase c)
        //{
        //    c.AttachedSettingsControlHelper.AttachedTarget = TargetObject;
        //}
        MainContentPresenter.Content = ContentObject;
        AssociatedAttachedSettings = settings as IAttachedSettings;
        UpdateSourceSettings(AssociatedAttachedSettings);

        if (!IsDependencyMode || ProfileAnalyzeService == null || !ProfileAnalyzeService.Nodes.TryGetValue(new AttachableObjectAddress(ContentId, ContentIndex), out var node))
        {
            return;
        }

        DependencyItemPackIconKind = node.Target switch
        {
            AttachedSettingsTargets.Lesson => '\uEFD7',
            AttachedSettingsTargets.Subject => '\uE47A',
            AttachedSettingsTargets.ClassPlan => '\ue6b1',
            AttachedSettingsTargets.TimePoint => '\uf35b',
            AttachedSettingsTargets.TimeLayout => '\uf0eb',
            _ => '\uef27'
        };
        var policy = ManagementService.Policy;
        IsEnabled = !policy.DisableProfileEditing;
        switch (node.Target)
        {
            case AttachedSettingsTargets.Lesson:
                if (ProfileService.Profile.ClassPlans.TryGetValue(ContentId, out var classPlan))
                {
                    DependencyItemTitle = $"课表 {classPlan.Name}，第{ContentIndex}节";
                }

                if (policy.DisableProfileClassPlanEditing)
                {
                    IsEnabled = false;
                }
                break;
            case AttachedSettingsTargets.ClassPlan:
                if (ProfileService.Profile.ClassPlans.TryGetValue(ContentId, out var classPlan2))
                {
                    DependencyItemTitle = $"课表 {classPlan2.Name}";
                }
                if (policy.DisableProfileClassPlanEditing)
                {
                    IsEnabled = false;
                }
                break;
            case AttachedSettingsTargets.TimePoint:
                if (ProfileService.Profile.TimeLayouts.TryGetValue(ContentId, out var timeLayout) && node.Object is TimeLayoutItem item)
                {
                    DependencyItemTitle = $"时间表 {timeLayout.Name}，{item.StartTime:t}-{item.EndTime:t}";
                }
                if (policy.DisableProfileTimeLayoutEditing)
                {
                    IsEnabled = false;
                }
                break;

            case AttachedSettingsTargets.Subject:
                if (node.Object is Subject subject)
                {
                    DependencyItemTitle = $"科目 {subject.Name}";
                }
                if (policy.DisableProfileSubjectsEditing)
                {
                    IsEnabled = false;
                }
                break;
            case AttachedSettingsTargets.TimeLayout:
                if (node.Object is TimeLayout item2)
                {
                    DependencyItemTitle = $"时间表 {item2.Name}";
                }
                if (policy.DisableProfileTimeLayoutEditing)
                {
                    IsEnabled = false;
                }
                break;
            case AttachedSettingsTargets.None:
            default:
                DependencyItemTitle = "???";
                break;
        }
    }

    private void UpdateSourceSettings(IAttachedSettings? settings)
    {
        if (settings?.IsAttachSettingsEnabled != true && ControlInfo.HasEnabledState)
        {
            // 在附加设置没有启用，且控件有附加设置启用状态的情况下不回写设置信息，以降低档案文件大小。
            return;
        }
        TargetObject.AttachedObjects[ControlInfo.Guid] = settings;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    #region PropertyChanged
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

    private void Popup_OnClosed(object? sender, EventArgs e)
    {
        NextItems.Clear();
        PreviousItems.Clear();
    }

    private async void ButtonRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        await AnalyzeAsync();
    }

    private void ButtonIsSettingsEnabled_OnClick(object? sender, RoutedEventArgs e)
    {
        UpdateSourceSettings(AssociatedAttachedSettings);
    }
}
