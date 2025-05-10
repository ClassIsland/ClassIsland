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
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Services.Management;
using ClassIsland.Shared.Models.Profile;

using Microsoft.Extensions.Logging;

using static ClassIsland.Shared.Helpers.ConfigureFileHelper;

using Path = System.IO.Path;
using ClassIsland.Shared;
using ClassIsland.Shared.IPC.Abstractions.Services;
using ClassIsland.Shared.Protobuf.AuditEvent;
using ClassIsland.Shared.Protobuf.Enum;
using dotnetCampus.Ipc.CompilerServices.GeneratedProxies;
using Sentry;

namespace ClassIsland.Services;

public class ProfileService : IProfileService, INotifyPropertyChanged
{
    public string CurrentProfilePath { 
        get; 
        set;
    } = Path.Combine(App.AppRootFolderPath, @"Default.json");

    public static readonly string ManagementClassPlanPath =
        Path.Combine(Management.ManagementService.ManagementConfigureFolderPath, "ClassPlans.json");

    public static readonly string ManagementTimeLayoutPath =
        Path.Combine(Management.ManagementService.ManagementConfigureFolderPath, "TimeLayouts.json");

    public static readonly string ManagementSubjectsPath =
        Path.Combine(Management.ManagementService.ManagementConfigureFolderPath, "Subjects.json");

    public static readonly string ProfilePath = Path.Combine(App.AppRootFolderPath, "Profiles");

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


    private async Task MergeManagementProfileAsync()
    {
        var span = SentrySdk.GetSpan();
        var spanLoadMgmtProfile = span?.StartChild("profile-mgmt-pull-profile");
        Logger.LogInformation("正在拉取集控档案");
        if (ManagementService.Connection == null)
            return;
        try
        {
            Profile? classPlan = null;
            Profile? timeLayouts = null;
            Profile? subjects = null;
            if (ManagementService.Manifest.ClassPlanSource.IsNewerAndNotNull(ManagementService.Versions.ClassPlanVersion))
            {
                var spanDownload = spanLoadMgmtProfile?.StartChild("profile-mgmt-download-classPlan");
                var cpOld = LoadConfig<Profile>(ManagementClassPlanPath);
                var cpNew = classPlan = await ManagementService.Connection.GetJsonAsync<Profile>(ManagementService.Manifest.ClassPlanSource.Value!);
                MergeDictionary(Profile.ClassPlans, cpOld.ClassPlans, cpNew.ClassPlans);
                MergeDictionary(Profile.ClassPlanGroups, cpOld.ClassPlanGroups, cpNew.ClassPlanGroups);
                spanDownload?.Finish();
            }
            if (ManagementService.Manifest.TimeLayoutSource.IsNewerAndNotNull(ManagementService.Versions.TimeLayoutVersion))
            {
                var spanDownload = spanLoadMgmtProfile?.StartChild("profile-mgmt-download-timeLayout");
                var tlOld = LoadConfig<Profile>(ManagementTimeLayoutPath);
                var tlNew = timeLayouts = await ManagementService.Connection.GetJsonAsync<Profile>(ManagementService.Manifest.TimeLayoutSource.Value!);
                MergeDictionary(Profile.TimeLayouts, tlOld.TimeLayouts, tlNew.TimeLayouts);
                spanDownload?.Finish();
            }
            if (ManagementService.Manifest.SubjectsSource.IsNewerAndNotNull(ManagementService.Versions.SubjectsVersion))
            {
                var spanDownload = spanLoadMgmtProfile?.StartChild("profile-mgmt-download-subjects");
                var subjectOld = LoadConfig<Profile>(ManagementSubjectsPath);
                var subjectNew = subjects = await ManagementService.Connection.GetJsonAsync<Profile>(ManagementService.Manifest.SubjectsSource.Value!);
                MergeDictionary(Profile.Subjects, subjectOld.Subjects, subjectNew.Subjects);
                spanDownload?.Finish();
            }

            var spanSaving = spanLoadMgmtProfile?.StartChild("profile-mgmt-save");
            SaveProfile("_management-profile.json");
            ManagementService.Versions.ClassPlanVersion = ManagementService.Manifest.ClassPlanSource.Version;
            ManagementService.Versions.TimeLayoutVersion = ManagementService.Manifest.TimeLayoutSource.Version;
            ManagementService.Versions.SubjectsVersion = ManagementService.Manifest.SubjectsSource.Version;
            ManagementService.SaveSettings();
            spanSaving?.Finish();
        }
        catch (Exception exp)
        {
            spanLoadMgmtProfile?.Finish(exp);
            Logger.LogError(exp, "拉取档案失败。");
        }

        
        //Profile = ConfigureFileHelper.CopyObject(Profile);
        Profile.Subjects = CopyObject(Profile.Subjects);
        Profile.TimeLayouts = CopyObject(Profile.TimeLayouts);
        Profile.ClassPlans = CopyObject(Profile.ClassPlans);
        Profile.RefreshTimeLayouts();
        Logger.LogTrace("成功拉取集控档案！");
        spanLoadMgmtProfile?.Finish();
    }

    public async Task LoadProfileAsync()
    {
        var span = SentrySdk.GetSpan();
        var spanLoadingProfile = span?.StartChild("profile-loading");
        var filename = ManagementService.IsManagementEnabled ? "_management-profile.json" : SettingsService.Settings.SelectedProfile;
        var path = Path.Combine(ProfilePath, filename);
        Logger.LogInformation("加载档案中：{}", path);
        if (!File.Exists(path))
        {
            Logger.LogInformation("档案不存在：{}", path);
            if (!ManagementService.IsManagementEnabled)  // 在集控模式下不需要默认科目
            {
                var subject = new StreamReader(Application.GetResourceStream(new Uri("/Assets/default-subjects.json", UriKind.Relative))!.Stream).ReadToEnd();
                Profile.Subjects = JsonSerializer.Deserialize<Profile>(subject)!.Subjects;
            }
            SaveProfile(filename);
        }

        var r = LoadConfig<Profile>(path);

        Profile = r;
        if (ManagementService.IsManagementEnabled)
        {
            await MergeManagementProfileAsync();
        }
        Profile.PropertyChanged += (sender, args) => SaveProfile(filename);

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
        CurrentProfilePath = filename;
        Logger.LogTrace("成功加载档案！信任：{}", IsCurrentProfileTrusted);
        CleanExpiredTempClassPlan();
        _isProfileLoaded = true;

        Profile.ClassPlans.CollectionChanged += (sender, args) => AuditProfileChangeEvent(AuditEvents.ClassPlanUpdated, args);
        Profile.TimeLayouts.CollectionChanged += (sender, args) => AuditProfileChangeEvent(AuditEvents.TimeLayoutUpdated, args);
        Profile.Subjects.CollectionChanged += (sender, args) => AuditProfileChangeEvent(AuditEvents.SubjectUpdated, args);
        spanLoadingProfile?.Finish();
    }

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
                ItemId = args.NewItems?[0] switch
                {
                    KeyValuePair<string, ClassPlan> cp => cp.Key,
                    KeyValuePair<string, TimeLayout> tl => tl.Key,
                    KeyValuePair<string, Subject> s => s.Key,
                    _ => throw new ArgumentOutOfRangeException()
                },
            });
        }
    }

    public void SaveProfile()
    {
        if (!_isProfileLoaded)
        {
            return;
        }
        if (CurrentProfilePath.Contains(".\\Profiles\\"))
        {
            var splittedFileName = CurrentProfilePath.Split("\\");
            var fileName = splittedFileName[splittedFileName.Length - 1];
            SaveProfile(fileName);
            return;
        }
        SaveProfile(CurrentProfilePath);
    }

    public void SaveProfile(string filename)
    {
        Logger.LogInformation("写入档案文件：{}", Path.Combine(ProfilePath, filename));
        SaveConfig(Path.Combine(ProfilePath, filename), Profile);
    }

    private static T DuplicateJson<T>(T o)
    {
        var json = JsonSerializer.Serialize(o);
        return JsonSerializer.Deserialize<T>(json)!;
    }

    public string? CreateTempClassPlan(string id, string? timeLayoutId=null, DateTime? enableDateTime = null)
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

        newCp.IsOverlay = true;
        newCp.TimeLayoutId = timeLayoutId;
        newCp.OverlaySourceId = id;
        newCp.Name += "（临时层）";
        newCp.OverlaySetupTime = date;
        Profile.IsOverlayClassPlanEnabled = true;
        var newId = Guid.NewGuid().ToString();
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
        if (Profile.OverlayClassPlanId == null || !Profile.ClassPlans.ContainsKey(Profile.OverlayClassPlanId))
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
            ConvertToStdClassPlan(Profile.OverlayClassPlanId);
        }
    }

    public void ConvertToStdClassPlan(string id)
    {
        Logger.LogInformation("将临时层课表转换为普通课表：{}", id);
        var today = IAppHost.GetService<IExactTimeService>().GetCurrentLocalDateTime().Date;
        if (!Profile.ClassPlans.TryGetValue(id, out var classPlan))
        {
            return;
        }
        classPlan.IsOverlay = false;
    }

    public void SetupTempClassPlanGroup(string key, DateTime? expireTime = null)
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