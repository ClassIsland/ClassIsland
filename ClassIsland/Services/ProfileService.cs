using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Platform;
using ClassIsland.Core.Extensions;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Services.Management;
using ClassIsland.Models.Profile;
using ClassIsland.Shared.Models.Profile;

using Microsoft.Extensions.Logging;

using static ClassIsland.Shared.Helpers.ConfigureFileHelper;

using Path = System.IO.Path;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.IPC.Abstractions.Services;
using ClassIsland.Shared.Protobuf.AuditEvent;
using ClassIsland.Shared.Protobuf.Enum;
using dotnetCampus.Ipc.CompilerServices.GeneratedProxies;
using Sentry;

namespace ClassIsland.Services;

public partial class ProfileService : IProfileService, INotifyPropertyChanged
{
    public string CurrentProfilePath { 
        get; 
        set;
    } = "Default.json";

    public static readonly string ManagementClassPlanPath =
        Path.Combine(Management.ManagementService.ManagementConfigureFolderPath, "ClassPlans.json");

    public static readonly string ManagementTimeLayoutPath =
        Path.Combine(Management.ManagementService.ManagementConfigureFolderPath, "TimeLayouts.json");

    public static readonly string ManagementSubjectsPath =
        Path.Combine(Management.ManagementService.ManagementConfigureFolderPath, "Subjects.json");

    public static readonly string ProfilePath = Path.Combine(CommonDirectories.AppRootFolderPath, "Profiles");

    public static readonly List<ProfileMigration> CurrentMigrations =
    [
        new()
        {
            Id = "classisland.subjects.extension1",
            RemoveOnDowngrade = true
        }
    ];

    public Profile Profile {
        get;
        set;
    } = new Profile();

    private SettingsService SettingsService { get; }

    private ILogger<ProfileService> Logger { get; }

    private IManagementService ManagementService { get; }
    public IIpcService IpcService { get; }

    private bool _isProfileLoaded = false;
    private bool _isCurrentProfileTrusted = false;

    public ProfileService(SettingsService settingsService, ILogger<ProfileService> logger, IManagementService managementService, IIpcService ipcService)
    {
        Logger = logger;
        ManagementService = managementService;
        IpcService = ipcService;
        SettingsService = settingsService;
        IpcService.IpcProvider.CreateIpcJoint<IPublicProfileService>(this);
        if (!Directory.Exists(ProfilePath))
        {
            Directory.CreateDirectory(ProfilePath);
        }
    }

    private async Task<Action?> MergeManagementProfileAsync(Profile profile)
    {
        var span = SentrySdk.GetSpan();
        var spanLoadMgmtProfile = span?.StartChild("profile-mgmt-pull-profile");
        Logger.LogInformation("正在拉取集控档案");
        if (ManagementService.Connection == null)
            return null;
        Action? commitVersions = null;
        try
        {
            Profile? classPlan = null;
            Profile? timeLayouts = null;
            Profile? subjects = null;
            if (ManagementService.Manifest.ClassPlanSource.IsNewerAndNotNull(ManagementService.Versions.ClassPlanVersion))
            {
                var spanDownload = spanLoadMgmtProfile?.StartChild("profile-mgmt-download-classPlan");
                var cpOld = File.Exists(ManagementClassPlanPath) ? ReadProfile(ManagementClassPlanPath) : new Profile();
                var cpNew = classPlan = await DownloadManagementProfileAsync(ManagementService.Manifest.ClassPlanSource.Value!);
                MergeDictionary(profile.ClassPlans, cpOld.ClassPlans, cpNew.ClassPlans);
                MergeDictionary(profile.ClassPlanGroups, cpOld.ClassPlanGroups, cpNew.ClassPlanGroups);
                spanDownload?.Finish();
            }
            if (ManagementService.Manifest.TimeLayoutSource.IsNewerAndNotNull(ManagementService.Versions.TimeLayoutVersion))
            {
                var spanDownload = spanLoadMgmtProfile?.StartChild("profile-mgmt-download-timeLayout");
                var tlOld = File.Exists(ManagementTimeLayoutPath) ? ReadProfile(ManagementTimeLayoutPath) : new Profile();
                var tlNew = timeLayouts = await DownloadManagementProfileAsync(ManagementService.Manifest.TimeLayoutSource.Value!);
                MergeDictionary(profile.TimeLayouts, tlOld.TimeLayouts, tlNew.TimeLayouts);
                spanDownload?.Finish();
            }
            if (ManagementService.Manifest.SubjectsSource.IsNewerAndNotNull(ManagementService.Versions.SubjectsVersion))
            {
                var spanDownload = spanLoadMgmtProfile?.StartChild("profile-mgmt-download-subjects");
                var subjectOld = File.Exists(ManagementSubjectsPath) ? ReadProfile(ManagementSubjectsPath) : new Profile();
                var subjectNew = subjects = await DownloadManagementProfileAsync(ManagementService.Manifest.SubjectsSource.Value!);
                MergeDictionary(profile.Subjects, subjectOld.Subjects, subjectNew.Subjects);
                spanDownload?.Finish();
            }

            var classPlanVersion = ManagementService.Manifest.ClassPlanSource.Version;
            var timeLayoutVersion = ManagementService.Manifest.TimeLayoutSource.Version;
            var subjectsVersion = ManagementService.Manifest.SubjectsSource.Version;
            commitVersions = () =>
            {
                ManagementService.Versions.ClassPlanVersion = classPlanVersion;
                ManagementService.Versions.TimeLayoutVersion = timeLayoutVersion;
                ManagementService.Versions.SubjectsVersion = subjectsVersion;
                ManagementService.SaveSettings();
            };
        }
        catch (ProfileLoadException exp)
        {
            spanLoadMgmtProfile?.Finish(exp);
            throw;
        }
        catch (Exception exp)
        {
            spanLoadMgmtProfile?.Finish(exp);
            Logger.LogError(exp, "拉取档案失败。");
        }

        //Profile = ConfigureFileHelper.CopyObject(Profile);
        profile.Subjects = CopyObject(profile.Subjects);
        profile.TimeLayouts = CopyObject(profile.TimeLayouts);
        profile.ClassPlans = CopyObject(profile.ClassPlans);
        profile.RefreshTimeLayouts();
        Logger.LogTrace("成功拉取集控档案！");
        spanLoadMgmtProfile?.Finish();
        return commitVersions;
    }

    public async Task LoadProfileAsync()
    {
        _isProfileLoaded = false;
        IsCurrentProfileTrusted = false;
        var span = SentrySdk.GetSpan();
        var spanLoadingProfile = span?.StartChild("profile-loading");
        var filename = ManagementService.IsManagementEnabled ? "_management-profile.json" : SettingsService.Settings.SelectedProfile;
        var path = Path.Combine(ProfilePath, filename);
        Logger.LogInformation("加载档案中：{}", path);
        try
        {
            var backupOriginal = File.Exists(path);
            Profile candidate;
            if (!backupOriginal)
            {
                candidate = CreateProfile(!ManagementService.IsManagementEnabled);
            }
            else
            {
                try
                {
                    candidate = ReadProfile(path);
                }
                catch (ProfileReadException exception) when (IsBackupEnabled && File.Exists(path + ".bak"))
                {
                    Logger.LogWarning(exception, "读取档案失败，尝试只读加载备份：{Path}", path);
                    candidate = ReadProfile(path + ".bak");
                    backupOriginal = false;
                }
            }

            var commitVersions = ManagementService.IsManagementEnabled
                ? await MergeManagementProfileAsync(candidate)
                : null;
            await ApplyMigrationsAsync(candidate);
            CommitLoadedProfile(path, candidate, backupOriginal && IsBackupEnabled);
            commitVersions?.Invoke();
            Profile.PropertyChanged -= ProfileOnPropertyChanged;
            Profile = candidate;
            CurrentProfilePath = filename;
        }
        catch (Exception exception)
        {
            spanLoadingProfile?.Finish(exception);
            Logger.LogError(exception, "档案加载未完成，已禁止保存：{Path}", path);
            if (exception is ProfileLoadException)
                throw;
            throw new ProfileLoadException("档案更新未完成。", exception);
        }

        if (SettingsService.Settings.TrustedProfileIds.Contains(Profile.Id))
        {
            IsCurrentProfileTrusted = true;
        }

        if (SettingsService.WillMigrateProfileTrustedState)
        {
            TrustCurrentProfile();
            SettingsService.WillMigrateProfileTrustedState = false;
            Logger.LogInformation("自动信任来自 1.5.4.0 以前的当前档案。");
        }
        Logger.LogTrace("成功加载档案！信任：{}", IsCurrentProfileTrusted);
        CleanExpiredTempClassPlan();
        _isProfileLoaded = true;
        Profile.PropertyChanged += ProfileOnPropertyChanged;

        Profile.ClassPlans.CollectionChanged += (sender, args) => AuditProfileChangeEvent(AuditEvents.ClassPlanUpdated, args);
        Profile.TimeLayouts.CollectionChanged += (sender, args) => AuditProfileChangeEvent(AuditEvents.TimeLayoutUpdated, args);
        Profile.Subjects.CollectionChanged += (sender, args) => AuditProfileChangeEvent(AuditEvents.SubjectUpdated, args);
        spanLoadingProfile?.Finish();
    }

    private void ProfileOnPropertyChanged(object? sender, PropertyChangedEventArgs args) => SaveProfile();

    public void AuditProfileChangeEvent(AuditEvents eventType, NotifyCollectionChangedEventArgs args)
    {
        if (ManagementService is { IsManagementEnabled: true, Connection: ManagementServerConnection connection })
        {
            connection.LogAuditEvent(eventType, new ProfileItemUpdated()
            {
                Operation = args.Action switch
                {
                    NotifyCollectionChangedAction.Add => ListItemUpdateOperations.Add,
                    NotifyCollectionChangedAction.Remove => ListItemUpdateOperations.Remove,
                    NotifyCollectionChangedAction.Replace => ListItemUpdateOperations.Update,
                    NotifyCollectionChangedAction.Move => ListItemUpdateOperations.Update,
                    NotifyCollectionChangedAction.Reset => ListItemUpdateOperations.Update,
                    _ => throw new ArgumentOutOfRangeException()
                },
                ItemId = (args.Action == NotifyCollectionChangedAction.Remove
                    ? args.OldItems?[0]
                    : args.NewItems?[0]) switch
                {
                    KeyValuePair<Guid, ClassPlan> cp => cp.Key.ToString(),
                    KeyValuePair<Guid, TimeLayout> tl => tl.Key.ToString(),
                    KeyValuePair<Guid, Subject> s => s.Key.ToString(),
                    _ => throw new ArgumentOutOfRangeException()
                },
            });
        }
    }

    public void SaveProfile()
    {
        SaveProfile(Path.GetFileName(CurrentProfilePath));
    }

    public void SaveProfile(string filename)
    {
        if (!_isProfileLoaded)
        {
            Logger.LogWarning("档案尚未成功加载，已阻止保存：{FileName}", filename);
            return;
        }
        Logger.LogInformation("写入档案文件：{}", Path.Combine(ProfilePath, filename));
        SaveConfig(Path.Combine(ProfilePath, filename), Profile);
    }

    private static T DuplicateJson<T>(T o)
    {
        var json = JsonSerializer.Serialize(o);
        return JsonSerializer.Deserialize<T>(json)!;
    }

    public Guid? CreateTempClassPlan(Guid id, Guid? timeLayoutId=null, DateTime? enableDateTime = null)
    {
        return CreateTempClassPlan(id, timeLayoutId, enableDateTime, false);
    }

    public Guid? CreateTempClassPlan(Guid id, Guid? timeLayoutId, DateTime? enableDateTime, bool createTempTimeLayout)
    {
        Logger.LogInformation("创建临时层：{}", id);
        var date = enableDateTime ?? IAppHost.GetService<IExactTimeService>().GetCurrentLocalDateTime().Date;
        if (Profile.OrderedSchedules.TryGetValue(date, out var orderedSchedule)
            && Profile.ClassPlans.TryGetValue(orderedSchedule.ClassPlanId, out var cp1)
            && cp1.IsOverlay)
        {
            return null;
        }
        var cp = Profile.ClassPlans[id];
        timeLayoutId ??= cp.TimeLayoutId;
        var newCp = DuplicateJson(cp);

        if (createTempTimeLayout && Profile.TimeLayouts.TryGetValue(timeLayoutId.Value, out var sourceLayout))
        {
            var newLayout = DuplicateJson(sourceLayout);
            newLayout.IsOverlay = true;
            newLayout.OverlaySourceId = timeLayoutId.Value;
            newLayout.Name += "（临时层）";
            var newLayoutId = Guid.NewGuid();
            Profile.TimeLayouts.Add(newLayoutId, newLayout);
            timeLayoutId = newLayoutId;
            Logger.LogInformation("同时创建临时时间表：{} -> {}", sourceLayout.Name, newLayoutId);
        }

        newCp.IsOverlay = true;
        newCp.TimeLayoutId = timeLayoutId.Value;
        newCp.OverlaySourceId = id;
        newCp.Name += "（临时层）";
        newCp.OverlaySetupTime = date;
        Profile.IsOverlayClassPlanEnabled = true;
        var newId = Guid.NewGuid();
        Profile.OverlayClassPlanId = newId;
        Profile.ClassPlans.Add(newId, newCp);
        Profile.OrderedSchedules[date] = new OrderedSchedule()
        {
            ClassPlanId = newId
        };
        return newId;
    }

    public void ClearTempClassPlan()
    {
        if (Profile.OverlayClassPlanId == null || !Profile.ClassPlans.ContainsKey(Profile.OverlayClassPlanId ?? Guid.Empty))
        {
            return;
        }

        Logger.LogInformation("清空今天的临时层：{}", Profile.OverlayClassPlanId);
        Profile.OrderedSchedules.Remove(IAppHost.GetService<IExactTimeService>().GetCurrentLocalDateTime().Date);
        Profile.OverlayClassPlanId = null;
        CleanExpiredTempClassPlan();
    }

    public void CleanExpiredTempClassPlan()
    {
        var today = IAppHost.GetService<IExactTimeService>().GetCurrentLocalDateTime().Date;
        foreach (var (key, _) in Profile.OrderedSchedules
                     .Where(x => x.Key < today)
                     .ToList())
        {
            Profile.OrderedSchedules.Remove(key);
            Logger.LogInformation("清理过期的课表预定：{}", key);
        }

        var orderedSchedules = Profile.OrderedSchedules.Select(x => x.Value.ClassPlanId).ToList();

        foreach (var (key, _) in Profile.ClassPlans.Where(x => x.Value.IsOverlay).ToList())
        {
            if (orderedSchedules.Contains(key)) 
                continue;
            Profile.ClassPlans.Remove(key);
            Logger.LogInformation("清理没有被引用的过期临时层课表：{}", key);
        }

        var activeTimeLayoutIds = Profile.ClassPlans
            .Where(x => !x.Value.IsOverlay || orderedSchedules.Contains(x.Key))
            .Select(x => x.Value.TimeLayoutId)
            .ToHashSet();

        foreach (var (key, layout) in Profile.TimeLayouts.Where(x => x.Value.IsOverlay).ToList())
        {
            if (activeTimeLayoutIds.Contains(key))
                continue;
            Profile.TimeLayouts.Remove(key);
            Logger.LogInformation("清理没有被引用的过期临时层时间表：{}", key);
        }
    }

    //[Obsolete]
    //public bool CheckClassPlan(ClassPlan plan)
    //{
    //}

    public void ConvertToStdClassPlan()
    {
        Logger.LogInformation("将当前临时层课表转换为普通课表：{}", Profile.OverlayClassPlanId);
        if (Profile.OverlayClassPlanId != null)
        {
            ConvertToStdClassPlan(Profile.OverlayClassPlanId ?? Guid.Empty);
        }
    }

    public void ConvertToStdClassPlan(Guid id)
    {
        Logger.LogInformation("将临时层课表转换为普通课表：{}", id);
        var today = IAppHost.GetService<IExactTimeService>().GetCurrentLocalDateTime().Date;
        if (!Profile.ClassPlans.TryGetValue(id, out var classPlan))
        {
            return;
        }
        classPlan.IsOverlay = false;

        if (Profile.TimeLayouts.TryGetValue(classPlan.TimeLayoutId, out var layout) && layout.IsOverlay)
        {
            layout.IsOverlay = false;
            layout.Name = layout.Name.Replace("（临时层）", "");
            Logger.LogInformation("同时将临时层时间表转换为普通时间表：{}", classPlan.TimeLayoutId);
        }
    }

    public void SetupTempClassPlanGroup(Guid key, DateTime? expireTime = null)
    {
        var classPlans = Profile.ClassPlans
            .Where(x => x.Value.AssociatedGroup == key)
            .Select(x => x.Value);
        var today = App.GetService<IExactTimeService>().GetCurrentLocalDateTime();
        var dow = today.DayOfWeek;
        var dayOffset = 0;
        var dd = today.Date - SettingsService.Settings.SingleWeekStartTime.Date;
        var dw = Math.Floor(dd.TotalDays / 7) + 1;
        foreach (var classPlan in classPlans)
        {
            var w = (int)dw % classPlan.TimeRule.WeekCountDivTotal;
            var baseOffset = (int)(classPlan.TimeRule.WeekDay - dow);
            var divOffset = (classPlan.TimeRule.WeekCountDiv + classPlan.TimeRule.WeekCountDivTotal - w) % classPlan.TimeRule.WeekCountDivTotal;
            var finalOffset = baseOffset + (divOffset * 7);
            if (finalOffset < 0)
            {
                finalOffset += 7;
            }

            dayOffset = Math.Max(finalOffset, dayOffset);
        }
        expireTime ??= App.GetService<IExactTimeService>().GetCurrentLocalDateTime().Date + TimeSpan.FromDays(dayOffset);

        Profile.TempClassPlanGroupExpireTime = expireTime.Value;
        Profile.TempClassPlanGroupId = key;
        Profile.IsTempClassPlanGroupEnabled = true;
    }

    public void ClearTempClassPlanGroup()
    {
        Profile.TempClassPlanGroupId = null;
        Profile.IsTempClassPlanGroupEnabled = false;
    }

    public bool IsCurrentProfileTrusted
    {
        get => _isCurrentProfileTrusted;
        private set
        {
            if (value == _isCurrentProfileTrusted) return;
            _isCurrentProfileTrusted = value;
            OnPropertyChanged();
        }
    }

    public void ClearExpiredTempClassPlanGroup()
    {
        if (Profile.TempClassPlanGroupExpireTime.Date < App.GetService<IExactTimeService>().GetCurrentLocalDateTime().Date)
        {
            ClearTempClassPlanGroup();
        }
    }

    public void TrustCurrentProfile()
    {
        SettingsService.Settings.TrustedProfileIds.Add(Profile.Id);
        IsCurrentProfileTrusted = true;
        Logger.LogInformation("已信任当前档案 {}", Profile.Id);
    }

    public static Profile CreateProfile(bool applyTemplate, string templateName = "default.json")
    {
        var profile = new Profile
        {
            Migrations = CopyObject(CurrentMigrations)
        };

        if (applyTemplate)
        {
            var json = AssetLoader.ReadAllText(new Uri($"avares://ClassIsland/Assets/ProfileTemplates/{templateName}"));
            var template = JsonSerializer.Deserialize<Profile>(json)!;
            profile.Subjects = CopyObject(template.Subjects);
            profile.TimeLayouts = CopyObject(template.TimeLayouts);
            profile.ClassPlans = CopyObject(template.ClassPlans);
            profile.ScheduleItems = CopyObject(template.ScheduleItems);
            profile.ClassPlanGroups = CopyObject(template.ClassPlanGroups);
            profile.OrderedSchedules = CopyObject(template.OrderedSchedules);
            profile.ScheduleType = template.ScheduleType;
        }

        return profile;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
