using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Labs.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.Xaml.Interactions.DragAndDrop;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Assists;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Controls.Ruleset;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Components;
using ClassIsland.Core.Models.UI;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.ViewModels.EditMode;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Controls.EditMode;

public partial class EditModeView : UserControl
{
    public EditModeViewModel ViewModel { get; } = IAppHost.GetService<EditModeViewModel>();
    
    public EditModeView()
    {
        InitializeComponent();
        DataContext = this;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        ClearSelectedComponents();
    }

    private void ClearSelectedComponents()
    {
        ViewModel.MainWindow.ViewModel.SelectedComponentSettings = null;
        ViewModel.MainWindow.ViewModel.SelectedMainWindowLineSettings = null;
        ViewModel.MainViewModel.ContainerComponents.Clear();
        ViewModel.ContainerComponentCache.Clear();
    }

    private void OpenDrawer(string key, string? title = null, string? icon = null)
    {
        OpenDrawerCore(this.FindResource(key), icon == null
            ? title
            : new IconText
            {
                Glyph = icon,
                Text = title ?? ""
            });
    }

    private void OpenDrawerCore(object? content, object? title)
    {
        ViewModel.MainDrawerContent = content;
        ViewModel.MainDrawerTitle = title;
        ViewModel.MainDrawerState = VerticalDrawerOpenState.Opened;
    }
    
    
    private void CommandBindingOpenDrawer_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ViewModel.SecondaryDrawerContent = e.Parameter;
        ViewModel.SecondaryDrawerState = VerticalDrawerOpenState.Opened;
    }

    private void CommandBindingCloseDrawer_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ViewModel.SecondaryDrawerState = VerticalDrawerOpenState.Closed;
    }

    public void OpenComponentsLibDrawer()
    {
        OpenDrawer("ComponentsDrawer", "组件库");
        // 重载组件库列表项目，修复切换视图后无法拖拽的问题。
        ViewModel.ComponentInfos = [];
        ViewModel.ComponentInfos = ComponentRegistryService.Registered;
    }
    
    public void OpenAppearanceSettingsDrawer()
    {
        OpenDrawer("AppearanceSettingsDrawer", "外观");
    }

    private void InstanceOnDragEnded()
    {
        if (!ViewModel.IsDrawerTempCollapsed)
        {
            return;
        }
        
        ViewModel.MainDrawerState = VerticalDrawerOpenState.Opened;
        ViewModel.IsDrawerTempCollapsed = false;
    }

    private void InstanceOnDragStarted()
    {
        if (ViewModel.MainDrawerState != VerticalDrawerOpenState.Opened)
        {
            return;
        }

        ViewModel.MainDrawerState = VerticalDrawerOpenState.Collapsed;
        ViewModel.IsDrawerTempCollapsed = true;
    }
    
    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        ManagedDragDropService.Instance.DragStarted += InstanceOnDragStarted;
        ManagedDragDropService.Instance.DragEnded += InstanceOnDragEnded;
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        ManagedDragDropService.Instance.DragStarted -= InstanceOnDragStarted;
        ManagedDragDropService.Instance.DragEnded -= InstanceOnDragEnded;
    }

    private void SettingsExpanderNavigateAppearance_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!ViewModel.MainWindow.ViewModel.IsWindowMode)
        {
            ViewModel.MainWindow.ViewModel.IsEditMode = false;
        }
        ViewModel.UriNavigationService.NavigateWrapped(new Uri("classisland://app/settings/appearance"));
    }

    public void ShowComponentSettings()
    {
        OpenDrawerCore(this.FindResource("ComponentSettingsDrawer"), 
            this.FindResource("ComponentSettingsDrawerTitle"));
    }
    
    public void OpenChildComponents(ComponentSettings? settings, IReadOnlyList<ComponentSettings> stack, Point pos)
    {
        if (settings == null)
        {
            return;
        }
        var info = new EditModeContainerComponentInfo(settings, [..stack, settings])
        {
            X = pos.X,
            Y = pos.Y
        };
        var success = ViewModel.ContainerComponentCache.TryAdd(settings, info);
        if (!success)
        {
            return;
        }
        ViewModel.MainViewModel.ContainerComponents.Add(info);
        
    }
    
    private void ButtonOpenRuleset_OnClick(object? sender, RoutedEventArgs e)
    {
        if (this.FindResource("RulesetControl") is not RulesetControl control ||
            ViewModel.MainViewModel.SelectedComponentSettings == null) 
            return;
        control.Ruleset = ViewModel.MainViewModel.SelectedComponentSettings.HidingRules;
        SettingsPageBase.OpenDrawerCommand.Execute(control);
        ViewModel.SecondaryDrawerContent = control;
        ViewModel.SecondaryDrawerTitle = "编辑规则集";
        ViewModel.SecondaryDrawerState = VerticalDrawerOpenState.Opened;
    }


    public void CloseContainerComponent(EditModeContainerComponentInfo? info)
    {
        if (info == null)
        {
            return;
        }

        ViewModel.MainViewModel.ContainerComponents.Remove(info);
        ViewModel.ContainerComponentCache.Remove(info.Settings);
    }

    public void OpenMainWindowLineSettings(MainWindowLineSettings settings)
    {
        ViewModel.SelectedMainWindowLineSettings = settings;
        OpenDrawer("MainWindowLineSettingsDrawer", "主界面行设置");
    }
    
    [RelayCommand]
    private void ComponentLayoutSelectionRadioButtonToggled(string name)
    {
        ViewModel.SettingsService.Settings.CurrentComponentConfig = name;
        ClearSelectedComponents();
    }

    private void ButtonOpenComponentLayoutsFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath(ClassIsland.Services.ComponentsService.ComponentSettingsPath),
            UseShellExecute = true
        });
    }

    private async void ButtonCreateComponentLayout_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.CreateProfileName = "";

        var textBox = new TextBox()
        {
            Text = ""
        };
        var dialogResult = await new ContentDialog()
        {
            Title = "创建组件配置",
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "创建",
            SecondaryButtonText = "取消",
            Content = new Field()
            {
                Content = textBox,
                Label = "组件名",
                Suffix = ".json"
            }
        }.ShowAsync();

        ViewModel.CreateProfileName = textBox.Text;
        var path = Path.Combine(ClassIsland.Services.ComponentsService.ComponentSettingsPath,
            ViewModel.CreateProfileName + ".json");
        if (dialogResult != ContentDialogResult.Primary || File.Exists(path))
        {
            return;
        }
        ConfigureFileHelper.SaveConfig(path, ClassIsland.Services.ComponentsService.DefaultComponentProfile);
        ViewModel.ComponentsService.RefreshConfigs();
        ViewModel.SettingsService.Settings.CurrentComponentConfig = ViewModel.CreateProfileName;
        ClearSelectedComponents();
    }

    private void ButtonRefreshComponentLayouts_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.ComponentsService.RefreshConfigs();
    }

    public void OpenComponentLayoutsManagerDrawer()
    {
        ViewModel.SecondaryDrawerTitle = this.FindResource("ComponentLayoutsManagerDrawerTitle");
        ViewModel.SecondaryDrawerContent = this.FindResource("ComponentLayoutsManagerDrawer");
        ViewModel.SecondaryDrawerState = VerticalDrawerOpenState.Opened;
    }

    [RelayCommand]
    private async Task DeleteComponentLayout(string name)
    {
        var path = Path.Combine(Services.ComponentsService.ComponentSettingsPath, $"{name}.json");
        if (name == ViewModel.SettingsService.Settings.CurrentComponentConfig)
        {
            this.ShowToast(new ToastMessage("无法删除已加载或将要加载的组件配置。")
            {
                Severity = InfoBarSeverity.Warning
            });
            return;
        }

        var textBox = new TextBox();
        var r = await new ContentDialog()
        {
            Title = "删除组件配置",
            Content = $"您确定要删除组件配置 {name} 吗？此操作无法撤销，组件配置内的组件信息都将被删除！",
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "删除",
            SecondaryButtonText = "取消"
        }.ShowAsync();

        if (r == ContentDialogResult.Primary)
        {
            File.Delete(path);
        }

        ViewModel.ComponentsService.RefreshConfigs();
    }

    [RelayCommand]
    private async Task RenameComponentLayout(string name)
    {
        var textBox = new TextBox()
        {
            Text = name
        };
        var r = await new ContentDialog()
        {
            Title = "重命名组件配置方案",
            Content = new Field()
            {
                Content = textBox,
                Label = "组件配置方案名称",
                Suffix = ".json"
            },
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "重命名",
            SecondaryButtonText = "取消"
        }.ShowAsync();

        var raw = Path.Combine(Services.ComponentsService.ComponentSettingsPath, $"{name}.json");
        var path = Path.Combine(Services.ComponentsService.ComponentSettingsPath, $"{textBox.Text}.json");
        if (r != ContentDialogResult.Primary || !File.Exists(raw))
        {
            return;
        }

        if (File.Exists(path))
        {
            this.ShowToast(new ToastMessage()
            {
                Message = "无法重命名组件配置，因为已存在一个相同名称的组件配置。",
                Severity = InfoBarSeverity.Warning
            });
            return;
        }

        File.Move(raw, path);
        if (ViewModel.SettingsService.Settings.CurrentComponentConfig == Path.GetFileNameWithoutExtension(raw))
        {
            ViewModel.SettingsService.Settings.CurrentComponentConfig = Path.GetFileNameWithoutExtension(path);
        }

        ViewModel.ComponentsService.RefreshConfigs();
    }

    [RelayCommand]
    private void DuplicateComponentLayout(string name)
    {
        var raw = Path.Combine(Services.ComponentsService.ComponentSettingsPath, $"{name}.json");
        var d = name + " - 副本.json";
        var d1 = Path.Combine(Services.ComponentsService.ComponentSettingsPath, $"{d}");
        File.Copy(raw, d1);
        ViewModel.ComponentsService.RefreshConfigs();
    }
}