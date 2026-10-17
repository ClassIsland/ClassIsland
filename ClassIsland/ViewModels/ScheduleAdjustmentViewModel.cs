using System;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Management;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ClassIsland.ViewModels;

internal partial class ScheduleAdjustmentViewModel : ObservableObject
{
    private readonly IProfileService _profileService;
    private readonly IManagementService _managementService;
    private readonly Profile _profile;
    private readonly Func<Task<bool>> _authorize;
    private readonly ILogger<ScheduleAdjustmentViewModel> _logger;

    public DateTime TargetDate { get; }
    public bool HasAdjustment => _profile.OrderedSchedulesV2.ContainsKey(TargetDate);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyDateCommand))]
    private DateTime? _sourceDate;

    public event Action<string>? Feedback;
    public event Action? Applied;

    public ScheduleAdjustmentViewModel(IProfileService profileService, IManagementService managementService,
        ILogger<ScheduleAdjustmentViewModel> logger, DateOnly date, Func<Task<bool>> authorize)
    {
        _profileService = profileService;
        _managementService = managementService;
        _logger = logger;
        _profile = profileService.Profile;
        _authorize = authorize;
        TargetDate = date.ToDateTime(TimeOnly.MinValue);
        SourceDate = _profile.OrderedSchedulesV2.TryGetValue(TargetDate, out var adjustment) && !adjustment.SetEmpty
            ? adjustment.SourceDate.Date : TargetDate;
    }

    public static bool CanEdit(Profile profile, ManagementPolicy policy) =>
        profile.ScheduleType == ScheduleType.Schedule
        && !policy.DisableProfileEditing
        && !policy.DisableProfileClassPlanEditing
        && !policy.DisableProfileTimeLayoutEditing
        && !policy.DisableProfileSubjectsEditing;

    private bool ValidateContext()
    {
        if (!ReferenceEquals(_profileService.Profile, _profile))
        {
            Feedback?.Invoke("当前档案已切换，请重新选择要调课的日期。");
            return false;
        }
        if (CanEdit(_profile, _managementService.Policy)) return true;
        Feedback?.Invoke("当前无法调整课程，请检查档案的编辑权限。");
        return false;
    }

    private bool CanApplyDate() => SourceDate.HasValue;

    [RelayCommand(CanExecute = nameof(CanApplyDate))]
    private Task ApplyDateAsync() => SaveAsync(new OrderedScheduleV2 { SourceDate = SourceDate!.Value.Date },
        $"已将 {TargetDate:M月d日} 调课为 {SourceDate:M月d日} 的课程。");

    [RelayCommand]
    private Task SetEmptyAsync() => SaveAsync(new OrderedScheduleV2 { SetEmpty = true },
        $"已将 {TargetDate:M月d日} 设为无课，可通过“移除调课”恢复。");

    [RelayCommand]
    private Task RemoveAsync() => SaveAsync(null, $"已移除 {TargetDate:M月d日} 的调课。");

    private async Task SaveAsync(OrderedScheduleV2? adjustment, string message)
    {
        if (!ValidateContext() || !await _authorize() || !ValidateContext()) return;
        _profile.OrderedSchedulesV2.TryGetValue(TargetDate, out var previous);
        if (adjustment == null) _profile.OrderedSchedulesV2.Remove(TargetDate);
        else _profile.OrderedSchedulesV2[TargetDate] = adjustment;
        try
        {
            _profileService.SaveProfile();
        }
        catch (Exception exception)
        {
            if (previous == null) _profile.OrderedSchedulesV2.Remove(TargetDate);
            else _profile.OrderedSchedulesV2[TargetDate] = previous;
            _logger.LogError(exception, "保存预定调课失败");
            Feedback?.Invoke("调课未能保存，请检查档案文件是否可以写入后重试。");
            return;
        }
        OnPropertyChanged(nameof(HasAdjustment));
        Applied?.Invoke();
        Feedback?.Invoke(message);
    }
}
