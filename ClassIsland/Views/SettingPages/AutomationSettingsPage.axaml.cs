using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Extensions;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Automation;
using ClassIsland.ViewModels.SettingsPages;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Workflow = ClassIsland.Core.Models.Automation.Workflow;
namespace ClassIsland.Views.SettingPages;

/// <summary>
///     AutomationSettingsPage.axaml 的交互逻辑
/// </summary>
[FullWidthPage]
[SettingsPageInfo("automation", "自动化", "\ueEF1", "\ueEF0", SettingsPageCategory.Internal)]
public partial class AutomationSettingsPage : SettingsPageBase
{
    public AutomationSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    public AutomationSettingsViewModel ViewModel { get; } = IAppHost.GetService<AutomationSettingsViewModel>();

    [RelayCommand]
    void DeleteWorkflow(Workflow workflow)
    {
        ViewModel.ActionService.InterruptActionSetAsync(workflow.ActionSet);

        var index = ViewModel.AutomationService.Workflows.IndexOf(workflow);
        ViewModel.AutomationService.Workflows.RemoveAt(index);
        ViewModel.SelectedWorkflow =
            ViewModel.AutomationService.Workflows.GetValueOrDefault(index) ??
            ViewModel.AutomationService.Workflows.GetValueOrDefault(index - 1);

        var revertButton = new Button
        {
            Content = "撤销"
        };
        var toastMessage = new ToastMessage($"已删除自动化“{workflow.ActionSet.Name}”。")
        {
            ActionContent = revertButton,
            Duration = TimeSpan.FromSeconds(10)
        };
        revertButton.Click += (o, args) =>
        {
            ViewModel.AutomationService.Workflows.Insert(Math.Min(index, ViewModel.AutomationService.Workflows.Count),
                workflow);
            ViewModel.SelectedWorkflow = workflow;
            toastMessage.Close();
        };
        this.ShowToast(toastMessage);
    }

    [RelayCommand]
    void DuplicateWorkflow(Workflow workflow)
    {
        var index = ViewModel.AutomationService.Workflows.IndexOf(workflow);
        var copy = ConfigureFileHelper.CopyObject(workflow);
        // 通过 Json 复制，多数属性已处理。
        copy.ActionSet.Guid = Guid.NewGuid();
        ViewModel.AutomationService.Workflows.Insert(index + 1, copy);
        ViewModel.SelectedWorkflow = copy;
    }

    [RelayCommand]
    void OnIsActionSetEnabledToggled(ActionSet actionSet)
    {
        // if (!actionSet.IsEnabled)
        //     ViewModel.ActionService.InterruptActionSetAsync(actionSet);
    }

    [RelayCommand]
    async Task CreateConfig()
    {
        var textBox = new TextBox
        {
            Text = ""
        };
        var dialogResult = await new ContentDialog
        {
            Title = "新建自动化配置方案",
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "创建",
            SecondaryButtonText = "取消",
            Content = new Field
            {
                Content = textBox,
                Label = "配置方案名称",
                Suffix = ".json"
            }
        }.ShowAsync();

        var createProfileName = textBox.Text;
        var path = Path.Combine(AutomationService.AutomationConfigsFolderPath,
            createProfileName + ".json");
        if (dialogResult != ContentDialogResult.Primary || File.Exists(path)) return;
        ConfigureFileHelper.SaveConfig(path, new ObservableCollection<Workflow>());
        ViewModel.AutomationService.RefreshConfigs();
        ViewModel.SettingsService.Settings.CurrentAutomationConfig = createProfileName;
    }

    [RelayCommand]
    void AddWorkflow()
    {
        var index = ViewModel.SelectedWorkflow == null
            ? ViewModel.AutomationService.Workflows.Count
            : ViewModel.AutomationService.Workflows.IndexOf(ViewModel.SelectedWorkflow) + 1;
        var workflow = new Workflow
        {
            ActionSet = new ActionSet
            {
                Name = "新自动化", IsRevertEnabled = true
            }
        };
        ViewModel.AutomationService.Workflows.Insert(index, workflow);
        ViewModel.SelectedWorkflow = workflow;
        ViewModel.IsPanelOpened = true;
    }

    void SelectorMain_OnSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
        {
            ViewModel.IsPanelOpened = false;
            return;
        }

        ViewModel.IsPanelOpened = true;
    }

    void RefreshWorkflows(object? sender, EventArgs eventArgs)
    {
        ViewModel.AutomationService.RefreshConfigs();
    }

    [RelayCommand]
    void OpenConfigFolder()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Path.GetFullPath(AutomationService.AutomationConfigsFolderPath),
            UseShellExecute = true
        });
    }

    [RelayCommand]
    void AgreeAutomationNotice()
    {
        ViewModel.SettingsService.Settings.IsAutomationEnabled = true;
        ViewModel.SettingsService.Settings.IsAutomationWarningVisible = false;
    }
}