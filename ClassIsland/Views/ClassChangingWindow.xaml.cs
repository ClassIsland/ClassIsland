using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Controls;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Services;
using ClassIsland.Services.Management;
using ClassIsland.Shared.Protobuf.AuditEvent;
using ClassIsland.Shared.Protobuf.Enum;
using ClassIsland.ViewModels;

using MaterialDesignThemes.Wpf;

namespace ClassIsland.Views;

/// <summary>
/// ClassChangingWindow.xaml 的交互逻辑
/// </summary>
public partial class ClassChangingWindow : MyWindow
{
    public ClassChangingViewModel ViewModel { get; } = new();

    public SettingsService SettingsService { get; } = App.GetService<SettingsService>();

    public IProfileService ProfileService { get; } = App.GetService<IProfileService>();

    public IManagementService ManagementService { get; } = App.GetService<IManagementService>();

    public static readonly DependencyProperty ClassPlanProperty = DependencyProperty.Register(
        nameof(ClassPlan), typeof(ClassPlan), typeof(ClassChangingWindow), new PropertyMetadata(default(ClassPlan)));

    public ClassPlan ClassPlan
    {
        get { return (ClassPlan)GetValue(ClassPlanProperty); }
        set { SetValue(ClassPlanProperty, value); }
    }
    private int GetSubjectIndex(int index)
    {
        var k = ClassPlan?.TimeLayout.Layouts[index];
        var l = (from t in ClassPlan?.TimeLayout.Layouts where t.TimeType == 0 select t).ToList();
        var i = l.IndexOf(k);
        return i;
    }

    public ClassChangingWindow()
    {
        //DataContext = this;
        InitializeComponent();
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.IsAutoNextStep || e.OriginalSource.GetType() == typeof(TabControl))
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
        var l = ProfileService.Profile.ClassPlans.Where(i => i.Value == ClassPlan).ToList();
        if (l.Count <= 0)
        {
            return;
        }
        var key = l[0].Key;
        if (!ViewModel.WriteToSourceClassPlan && !ClassPlan.IsOverlay && ProfileService.Profile.OverlayClassPlanId != null)
        {
            var r = (bool?)await DialogHost.Show(FindResource("OverwriteConfirm"), ViewModel.DialogIdentifier);
            if (r != true)
            {
                return;
            }
            ProfileService.ClearTempClassPlan();
        }

        if (!ViewModel.WriteToSourceClassPlan && !ClassPlan.IsOverlay)
        {
            key = ProfileService.CreateTempClassPlan(key);
        }

        if (key == null)
        {
            return;
        }
        var cp = ProfileService.Profile.ClassPlans[key];
        var aI = GetSubjectIndex(ViewModel.SourceIndex);
        var bI = 0;
        var a = "";
        var b = "";

        if (SettingsService.Settings.IsSwapMode)
        {
            bI = GetSubjectIndex(ViewModel.SwapModeTargetIndex);
            a = cp.Classes[aI].SubjectId;
            b = cp.Classes[bI].SubjectId;
            cp.Classes[aI].SubjectId = b;
            cp.Classes[bI].SubjectId = a;
        }
        else
        {
            if (ViewModel.TargetSubjectIndex == null)
            {
                return;
            }

            cp.Classes[aI].SubjectId = ViewModel.TargetSubjectIndex;
        }

        ProfileService.SaveProfile();
        if (ManagementService is { IsManagementEnabled: true, Connection: ManagementServerConnection connection })
        {
            connection.LogAuditEvent(AuditEvents.ClassChangeCompleted, new ClassChangeCompleted()
            {
                ChangeMode = SettingsService.Settings.IsSwapMode ? 0 : 1,
                ClassPlanId = key,
                SourceClassIndex = aI,
                SourceClassSubjectId = a,
                TargetClassIndex = bI,
                TargetClassSubjectId = b,
                WriteToSourceClassPlan = ViewModel.WriteToSourceClassPlan
            });
        }
        Close();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        DataContext = this;
        base.OnContentRendered(e);
    }

    private void ButtonTemporaryClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<ProfileSettingsWindow>().OpenDrawer("TemporaryClassPlan");
        App.GetService<ProfileSettingsWindow>().Open();
        Close();
    }
}