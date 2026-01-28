using System;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Controls.ScheduleDataGrid;

public partial class ScheduleDataGridColHeaderViewModel : ObservableObject
{
    [ObservableProperty] private string _classPlanName = "";
    [ObservableProperty] private string _suggestedClassPlanName = "";
    [ObservableProperty] private Guid _classPlanTimeLayoutId = Guid.Empty;
    [ObservableProperty] private TimeRule _classPlanTimeRule = new();

    [ObservableProperty] private bool _isOverlayCreatingPopupOpen = false;
    [ObservableProperty] private bool _isDeleteConfirmPopupOpen = false;

    [ObservableProperty] private Guid _tempOverlayClassPlanTimeLayoutId = Guid.Empty;
    [ObservableProperty] private DateTime _overlayEnableDateTime = DateTime.Now;
}