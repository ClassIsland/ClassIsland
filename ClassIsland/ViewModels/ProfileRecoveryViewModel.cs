using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClassIsland.Models.Profile;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ClassIsland.ViewModels;

public partial class ProfileRecoveryViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly ILogger<ProfileRecoveryViewModel> _logger;

    public string Message { get; }
    public bool CanSwitchProfile { get; }
    public ObservableCollection<string> Profiles { get; } = [];
    public string? ResultProfile { get; private set; }
    public bool RestoreBackupRequested { get; private set; }
    public event EventHandler? CloseRequested;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectProfileCommand))]
    private string? _selectedProfile;

    [ObservableProperty] private string _errorMessage = "";

    internal ProfileRecoveryViewModel(string rejectedProfile, bool isManaged, bool isDowngrade,
        SettingsService settingsService, ILogger<ProfileRecoveryViewModel> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
        CanSwitchProfile = !isManaged;
        Message = isManaged
            ? "学校统一提供的档案无法在当前版本中使用。您可以尝试恢复备份、使用兼容的 ClassIsland 版本，或联系管理员。"
            : isDowngrade
                ? "此档案由较新版本更新，当前版本无法使用。请选择其他档案、新建档案，或恢复备份。"
                : "档案更新未完成，暂时无法继续启动。您可以选择其他档案、新建档案，或恢复备份。";
        if (isManaged)
            return;

        try
        {
            foreach (var path in Directory.EnumerateFiles(ProfileService.ProfilePath, "*.json").OrderBy(Path.GetFileName))
            {
                var name = Path.GetFileName(path);
                if (!string.Equals(name, Path.GetFileName(rejectedProfile), StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(name, "_management-profile.json", StringComparison.OrdinalIgnoreCase))
                    Profiles.Add(name);
            }
            if (Profiles.Count == 0)
                ErrorMessage = "没有其他可选档案。您可以新建档案，或恢复备份。";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "无法列出恢复时可用的档案。");
            ErrorMessage = "无法读取档案列表。请检查档案文件夹是否可以访问，或退出后重试。";
        }
    }

    private bool CanSelectProfile() => CanSwitchProfile && SelectedProfile != null && Profiles.Contains(SelectedProfile);

    [RelayCommand(CanExecute = nameof(CanSelectProfile))]
    private void SelectProfile()
    {
        if (!CanSelectProfile())
            return;
        try
        {
            ProfileService.ReadProfile(Path.Combine(ProfileService.ProfilePath, SelectedProfile!));
            CompleteSelection(SelectedProfile!);
        }
        catch (ProfileDowngradeException exception)
        {
            _logger.LogWarning(exception, "所选档案不支持当前版本。");
            ErrorMessage = "所选档案也需要较新版本。请选择其他档案，或新建档案继续。";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "无法使用所选档案。");
            ErrorMessage = "无法使用所选档案。请检查文件是否可读取、设置是否可保存，或选择其他档案。";
        }
    }

    [RelayCommand]
    private void CreateProfile()
    {
        if (!CanSwitchProfile)
            return;
        try
        {
            var name = $"新档案-{Guid.NewGuid():N}.json";
            var path = Path.Combine(ProfileService.ProfilePath, name);
            var profile = ProfileService.CreateProfile(true);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            {
                JsonSerializer.Serialize(stream, profile);
                stream.Flush(true);
            }
            Profiles.Add(name);
            SelectedProfile = name;
            CompleteSelection(name);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "无法新建恢复档案。");
            ErrorMessage = "无法新建档案或保存选择。请检查剩余磁盘空间和文件夹写入权限，然后重试。";
        }
    }

    private void CompleteSelection(string filename)
    {
        _settingsService.Settings.SelectedProfile = filename;
        _settingsService.SaveSettings("选择启动恢复档案。");
        ResultProfile = filename;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void RestoreBackup()
    {
        RestoreBackupRequested = true;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Exit() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
