using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Extensions.UI;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Extensions;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;

namespace ClassIsland.Views.MainViewParts;

public partial class ScheduleModeView : ViewBase
{
    private readonly IProfileService _profileService = IAppHost.GetService<IProfileService>();
    private bool _initialized;

    public ScheduleModeView()
    {
        InitializeComponent();
        ClassicRadio.IsChecked = _profileService.Profile.ScheduleType == ScheduleType.Classic;
        ScheduleRadio.IsChecked = _profileService.Profile.ScheduleType == ScheduleType.Schedule;
        _initialized = true;
    }

    private void ModeRadio_OnChecked(object? sender, RoutedEventArgs e)
    {
        if (!_initialized || sender is not RadioButton { IsChecked: true } radio)
        {
            return;
        }

        var value = ReferenceEquals(radio, ScheduleRadio) ? ScheduleType.Schedule : ScheduleType.Classic;
        if (_profileService.Profile.ScheduleType == value)
        {
            return;
        }

        var previous = _profileService.Profile.ScheduleType;
        _profileService.Profile.ScheduleType = value;
        try
        {
            _profileService.SaveProfile(() => _profileService.Profile.ScheduleType = previous);
            ToastsHelper.ShowToast(this, value == ScheduleType.Schedule ? "已切换到日程模式。" : "已切换到经典模式。");
        }
        catch (Exception exception)
        {
            ClassicRadio.IsChecked = previous == ScheduleType.Classic;
            ScheduleRadio.IsChecked = previous == ScheduleType.Schedule;
            ToastsHelper.ShowErrorToast(this, "无法切换课程模式", exception);
        }
    }
}
