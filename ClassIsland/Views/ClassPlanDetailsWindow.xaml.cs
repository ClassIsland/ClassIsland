using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Models.ProfileAnalyzing;
using ClassIsland.Models;
using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;
using ClassIsland.ViewModels;

namespace ClassIsland.Views;

/// <summary>
/// ClassPlanDetailsWindow.xaml 的交互逻辑
/// </summary>
public partial class ClassPlanDetailsWindow
{
    public IProfileService ProfileService { get; }
    public IProfileAnalyzeService ProfileAnalyzeService { get; }

    public ClassPlanDetailsViewModel ViewModel { get; } = new();

    public ClassPlanDetailsWindow(IProfileService profileService, IProfileAnalyzeService profileAnalyzeService)
    {
        ProfileService = profileService;
        ProfileAnalyzeService = profileAnalyzeService;
        InitializeComponent();
        DataContext = this;
        ViewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(ViewModel.ClassPlan))
            {
                RefreshClasses();
            }

            if (args.PropertyName is nameof(ViewModel.SelectedControlInfo) or nameof(ViewModel.SelectedLesson))
            {
                Analyze();
            }
        };
    }

    public void RefreshClasses()
    {
        var cIndex = -1;
        ViewModel.Classes.Clear();
        ViewModel.ClassPlan.RefreshClassesList();
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ViewModel.ClassPlan.TimeLayout == null)
        {
            return;
        }
        foreach (var i in ViewModel.ClassPlan.TimeLayout.Layouts)
        {
            var info = new LessonDetails()
            {
                TimeLayoutItem = i
            };
            switch (i.TimeType)
            {
                case 0 when cIndex + 1 < ViewModel.ClassPlan.Classes.Count:
                {
                    cIndex++;
                    var classInfo = ViewModel.ClassPlan.Classes[cIndex];
                    if (ProfileService.Profile.Subjects.TryGetValue(classInfo.SubjectId, out var subject))
                    {
                        info.Subject = subject;
                        info.SubjectId = classInfo.SubjectId;
                    }
                    ViewModel.Classes.Add(info);
                    break;
                }
                case 1:
                    info.Subject = new()
                    {
                        Initial = "休",
                        Name = info.TimeLayoutItem.BreakNameText
                    };
                    ViewModel.Classes.Add(info);
                    break;
            }
        }
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshClasses();
    }

    private void Analyze()
    {
        if (ViewModel.SelectedControlInfo == null || ViewModel.SelectedLesson == null)
        {
            return;
        }

        ViewModel.Nodes.Clear();
        ViewModel.DisplayControlInfo = ViewModel.SelectedControlInfo;
        ProfileAnalyzeService.Analyze();
        var id = ViewModel.SelectedControlInfo.Guid;

        var state = AttachedSettingsControlState.Disabled;
        var nodes = new Dictionary<AttachableObjectAddress, AttachableSettingsObject>()
        {
            {
                new AttachableObjectAddress(ViewModel.ClassPlan.TimeLayoutId,
                    ViewModel.ClassPlan.TimeLayout.Layouts.IndexOf(ViewModel.SelectedLesson.TimeLayoutItem)),
                ViewModel.SelectedLesson.TimeLayoutItem
            },
            {
                new AttachableObjectAddress(ProfileService.Profile.ClassPlans
                    .FirstOrDefault(x => x.Value == ViewModel.ClassPlan).Key ?? ""),
                ViewModel.ClassPlan
            },
            {
                new AttachableObjectAddress(ViewModel.ClassPlan.TimeLayoutId), ViewModel.ClassPlan.TimeLayout
            },
        };
        if (ViewModel.SelectedLesson.TimeLayoutItem.TimeType != 1)
        {
            nodes.Add(
                new AttachableObjectAddress(ViewModel.SelectedLesson.SubjectId), ViewModel.SelectedLesson.Subject
            );
        }

        ViewModel.Summary = "小结：将使用在【应用设置】中的设置或默认设置。";
        foreach (var i in nodes)
        {
            if (!ProfileAnalyzeService.Nodes.TryGetValue(i.Key, out var node))
            {
                continue;
            }
            if (!ViewModel.SelectedControlInfo.Targets.HasFlag(node.Target))
            {
                continue;
            }
            var item = new AttachedSettingsNodeWithState()
            {
                Address = i.Key,
                Node = node
            };
            
            if (!i.Value.AttachedObjects.TryGetValue(id.ToString(), out var settings))
            {
                item.State = AttachedSettingsControlState.Disabled;
                goto finish;
            }

            if (IAttachedSettings.GetIsEnabled(settings))
            {
                item.State = AttachedSettingsControlState.Enabled;
                ViewModel.Summary = $"小结：将使用在{
                    node.Target switch {
                        AttachedSettingsTargets.None => "???", AttachedSettingsTargets.Lesson => "课程", AttachedSettingsTargets.Subject => "科目", AttachedSettingsTargets.TimePoint => "时间点", AttachedSettingsTargets.ClassPlan => "课表", AttachedSettingsTargets.TimeLayout => "时间表", _ => "???" }
                }的设置。";
            }
            else
                item.State = AttachedSettingsControlState.Disabled;

            finish:
            ViewModel.Nodes.Add(item);
        }

        ViewModel.Nodes =
            new ObservableCollection<AttachedSettingsNodeWithState>(ViewModel.Nodes.OrderBy(x => x.Node.Target));
        foreach (var i in ViewModel.Nodes)
        {
            if (i.State == AttachedSettingsControlState.Enabled && state == AttachedSettingsControlState.Override)
            {
                i.State = AttachedSettingsControlState.Override;
            }

            if (i.State == AttachedSettingsControlState.Enabled)
            {
                state = AttachedSettingsControlState.Override;
            }
        }
    }

    private void ButtonRefresh2_OnClick(object sender, RoutedEventArgs e)
    {
        Analyze();
    }
}