﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Models.ProfileAnalyzing;
using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Profile;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Core.Controls;

/// <summary>
/// AttachedSettingsControlPresenter.xaml 的交互逻辑
/// </summary>
public partial class AttachedSettingsControlPresenter : UserControl, INotifyPropertyChanged
{
    public IProfileAnalyzeService? ProfileAnalyzeService { get; }
    public IProfileService ProfileService { get; }
    public IManagementService ManagementService { get; }

    public static readonly DependencyProperty ControlInfoProperty = DependencyProperty.Register(
        nameof(ControlInfo), typeof(AttachedSettingsControlInfo), typeof(AttachedSettingsControlPresenter), new PropertyMetadata(default(AttachedSettingsControlInfo)));

    public AttachedSettingsControlInfo? ControlInfo
    {
        get => (AttachedSettingsControlInfo?)GetValue(ControlInfoProperty);
        set => SetValue(ControlInfoProperty, value);
    }

    public static readonly DependencyProperty TargetObjectProperty = DependencyProperty.Register(
        nameof(TargetObject), typeof(AttachableSettingsObject), typeof(AttachedSettingsControlPresenter), new PropertyMetadata(default(AttachableSettingsObject)));

    public AttachableSettingsObject? TargetObject
    {
        get => (AttachableSettingsObject)GetValue(TargetObjectProperty);
        set => SetValue(TargetObjectProperty, value);
    }

    public static readonly DependencyProperty ContentObjectProperty = DependencyProperty.Register(
        nameof(ContentObject), typeof(object), typeof(AttachedSettingsControlPresenter), new PropertyMetadata(default(object)));

    public object? ContentObject
    {
        get => (object)GetValue(ContentObjectProperty);
        set => SetValue(ContentObjectProperty, value);
    }

    public static readonly DependencyProperty AssociatedAttachedSettingsProperty = DependencyProperty.Register(
        nameof(AssociatedAttachedSettings), typeof(IAttachedSettings), typeof(AttachedSettingsControlPresenter), new PropertyMetadata(default(IAttachedSettings?)));

    public IAttachedSettings? AssociatedAttachedSettings
    {
        get { return (IAttachedSettings?)GetValue(AssociatedAttachedSettingsProperty); }
        set { SetValue(AssociatedAttachedSettingsProperty, value); }
    }

    public static readonly DependencyProperty ContentIdProperty = DependencyProperty.Register(
        nameof(ContentId), typeof(string), typeof(AttachedSettingsControlPresenter), new PropertyMetadata(default(string)));

    public string ContentId
    {
        get { return (string)GetValue(ContentIdProperty); }
        set { SetValue(ContentIdProperty, value); }
    }

    public static readonly DependencyProperty ContentIndexProperty = DependencyProperty.Register(
        nameof(ContentIndex), typeof(int), typeof(AttachedSettingsControlPresenter), new PropertyMetadata(-1));

    public int ContentIndex
    {
        get { return (int)GetValue(ContentIndexProperty); }
        set { SetValue(ContentIndexProperty, value); }
    }

    public static readonly DependencyProperty IsPopupOpenedProperty = DependencyProperty.Register(
        nameof(IsPopupOpened), typeof(bool), typeof(AttachedSettingsControlPresenter), new PropertyMetadata(default(bool)));

    private ObservableCollection<AttachableObjectNode> _nextItems = new();
    private ObservableCollection<AttachableObjectNode> _previousItems = new();
    private PackIconKind _dependencyItemPackIconKind = PackIconKind.CogOutline;
    private string _dependencyItemTitle = "";
    private bool _isLoading = false;

    public bool IsPopupOpened
    {
        get { return (bool)GetValue(IsPopupOpenedProperty); }
        set { SetValue(IsPopupOpenedProperty, value); }
    }

    public static readonly DependencyProperty IsDependencyModeProperty = DependencyProperty.Register(
        nameof(IsDependencyMode), typeof(bool), typeof(AttachedSettingsControlPresenter), new PropertyMetadata(default(bool)));

    public bool IsDependencyMode
    {
        get { return (bool)GetValue(IsDependencyModeProperty); }
        set { SetValue(IsDependencyModeProperty, value); }
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

    public PackIconKind DependencyItemPackIconKind
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

    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
        nameof(State), typeof(AttachedSettingsControlState), typeof(AttachedSettingsControlPresenter), new PropertyMetadata(AttachedSettingsControlState.Enabled));

    public AttachedSettingsControlState State
    {
        get { return (AttachedSettingsControlState)GetValue(StateProperty); }
        set { SetValue(StateProperty, value); }
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

    private void PopupBox_OnOpened(object sender, RoutedEventArgs e)
    {

    }

    private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
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
                guid.ToString())!);
            PreviousItems = new ObservableCollection<AttachableObjectNode>(ProfileAnalyzeService.FindPreviousObjects(
                new AttachableObjectAddress(contentId, contentIndex),
                guid.ToString())!);
        });
        IsLoading = false;
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
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

        TargetObject.AttachedObjects.TryGetValue(ControlInfo.Guid.ToString(), out var settings);
        ContentObject = AttachedSettingsControlBase.GetInstance(ControlInfo, ref settings);
        //if (ContentObject is IAttachedSettingsControlBase c)
        //{
        //    c.AttachedSettingsControlHelper.AttachedTarget = TargetObject;
        //}
        MainContentPresenter.Content = ContentObject;
        TargetObject.AttachedObjects[ControlInfo.Guid.ToString()] = settings;
        AssociatedAttachedSettings = settings as IAttachedSettings;

        if (!IsDependencyMode || ProfileAnalyzeService == null || !ProfileAnalyzeService.Nodes.TryGetValue(new AttachableObjectAddress(ContentId, ContentIndex), out var node))
        {
            return;
        }

        DependencyItemPackIconKind = node.Target switch
        {
            AttachedSettingsTargets.Lesson => PackIconKind.TextBoxOutline,
            AttachedSettingsTargets.Subject => PackIconKind.BookOutline,
            AttachedSettingsTargets.ClassPlan => PackIconKind.FileChartOutline,
            AttachedSettingsTargets.TimePoint => PackIconKind.TimelineOutline,
            AttachedSettingsTargets.TimeLayout => PackIconKind.TableClock,
            _ => PackIconKind.CogOutline
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
                    DependencyItemTitle = $"时间表 {timeLayout.Name}，{item.StartSecond:t}-{item.EndSecond:t}";
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
                DependencyItemTitle = "???";
                break;
            default:
                DependencyItemTitle = "???";
                break;
        }


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
}