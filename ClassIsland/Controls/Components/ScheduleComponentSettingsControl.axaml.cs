using System;
using System.Linq;
using System.Windows;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Models.ComponentSettings;
using ClassIsland.Services;

namespace ClassIsland.Controls.Components;

/// <summary>
/// ScheduleComponentSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class ScheduleComponentSettingsControl : ComponentBase<LessonControlSettings>
{
    public SettingsService SettingsService { get; }

    public ScheduleComponentSettingsControl(SettingsService settingsService)
    {
        SettingsService = settingsService;
        InitializeComponent();
    }

    private void ButtonShowAttachedSettings_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsPageBase.OpenDrawerCommand.Execute(new RootAttachedSettingsDependencyControl(IAttachedSettingsHostService.RegisteredControls.First(x => x.Guid == new Guid("58e5b69a-764a-472b-bcf7-003b6a8c7fdf"))));
    }
}
