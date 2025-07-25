using System;
using System.Linq;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Services;
using ClassIsland.Services.Management;
using ClassIsland.Shared;
using ClassIsland.Shared.Protobuf.AuditEvent;
using ClassIsland.Shared.Protobuf.Enum;
using ClassIsland.ViewModels;
using FluentAvalonia.UI.Controls;


namespace ClassIsland.Views;

/// <summary>
/// ClassChangingWindow.xaml 的交互逻辑
/// </summary>
public partial class ClassChangingWindow : MyWindow
{
    public ClassChangingViewModel ViewModel { get; } = IAppHost.GetService<ClassChangingViewModel>();

    private ClassPlan _classPlan;

    public static readonly DirectProperty<ClassChangingWindow, ClassPlan> ClassPlanProperty = AvaloniaProperty.RegisterDirect<ClassChangingWindow, ClassPlan>(
        nameof(ClassPlan), o => o.ClassPlan, (o, v) => o.ClassPlan = v);

    public ClassPlan ClassPlan
    {
        get => _classPlan;
        set => SetAndRaise(ClassPlanProperty, ref _classPlan, value);
    }
    
    private int GetSubjectIndex(int index)
    {
        var k = ClassPlan?.TimeLayout?.Layouts[index];
        var l = (from t in ClassPlan?.TimeLayout?.Layouts where t.TimeType == 0 select t).ToList();
        var i = l.IndexOf(k);
        return i;
    }

    public ClassChangingWindow()
    {
        DataContext = this;
        InitializeComponent();
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var aI = GetSubjectIndex(ViewModel.SourceIndex);
        if (aI < 0 || aI >= ClassPlan.Classes.Count)
        {
            this.ShowWarningToast("选择的课程无效。");
            return;
        }
        ViewModel.SelectedClassInfo = ClassPlan.Classes[aI];
        ViewModel.SelectedTimeLayoutItem = ClassPlan.TimeLayout?.Layouts[ViewModel.SourceIndex];
            if (ViewModel.IsAutoNextStep)
            return;
        ViewModel.IsAutoNextStep = true;
        ViewModel.SlideIndex = 1;
    }

    private void ButtonPrev_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SlideIndex = 0;
    }

    private void ButtonNext_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SlideIndex = 1;
    }

    private async void ButtonConfirmClassChanging_OnClick(object sender, RoutedEventArgs e)
    {
        var l = ViewModel.ProfileService.Profile.ClassPlans.Where(i => i.Value == ClassPlan).ToList();
        if (l.Count <= 0)
        {
            return;
        }
        var key = l[0].Key;
        if (!ViewModel.WriteToSourceClassPlan && !ClassPlan.IsOverlay && ViewModel.ProfileService.Profile.OverlayClassPlanId != null)
        {
            var r = await new ContentDialog()
            {
                Title = "覆盖当前的临时层",
                Content = "当前已经存在一个临时层课表，如果继续换课，那么该临时层将被覆盖。是否继续？",
                PrimaryButtonText = "继续",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            }.ShowAsync();
            if (r != ContentDialogResult.Primary)
            {
                return;
            }
            ViewModel.ProfileService.ClearTempClassPlan();
        }

        if (!ViewModel.WriteToSourceClassPlan && !ClassPlan.IsOverlay)
        {
            key = ViewModel.ProfileService.CreateTempClassPlan(key) ?? Guid.Empty;
        }

        if (key == Guid.Empty)
        {
            return;
        }
        var cp = ViewModel.ProfileService.Profile.ClassPlans[key];
        var aI = GetSubjectIndex(ViewModel.SourceIndex);
        var bI = 0;
        if (aI < 0 || aI >= ClassPlan.Classes.Count)
        {
            this.ShowWarningToast("选择的课程无效。");
            return;
        }
        var a = Guid.Empty;
        var b = Guid.Empty;

        if (ViewModel.SettingsService.Settings.IsSwapMode)
        {
            bI = GetSubjectIndex(ViewModel.SwapModeTargetIndex);
            if (bI < 0 || bI >= ClassPlan.Classes.Count)
            {
                this.ShowWarningToast("选择的课程无效。");
                return;
            }
            a = cp.Classes[aI].SubjectId;
            b = cp.Classes[bI].SubjectId;
            cp.Classes[aI].SubjectId = b;
            cp.Classes[bI].SubjectId = a;
        }
        else
        {
            if (ViewModel.TargetSubjectIndex == Guid.Empty)
            {
                return;
            }

            cp.Classes[aI].SubjectId = ViewModel.TargetSubjectIndex;
        }

        ViewModel.ProfileService.SaveProfile();
        if (ViewModel.ManagementService is { IsManagementEnabled: true, Connection: ManagementServerConnection connection })
        {
            connection.LogAuditEvent(AuditEvents.ClassChangeCompleted, new ClassChangeCompleted()
            {
                ChangeMode = ViewModel.SettingsService.Settings.IsSwapMode ? 0 : 1,
                ClassPlanId = key.ToString(),
                SourceClassIndex = aI,
                SourceClassSubjectId = a.ToString(),
                TargetClassIndex = bI,
                TargetClassSubjectId = b.ToString(),
                WriteToSourceClassPlan = ViewModel.WriteToSourceClassPlan
            });
        }
        Close();
    }

    private void ButtonTemporaryClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<ProfileSettingsWindow>().OpenDrawer("TemporaryClassPlan");
        App.GetService<ProfileSettingsWindow>().Open();
        Close();
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        DataContext = null;
    }

    private void ButtonCancel_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

