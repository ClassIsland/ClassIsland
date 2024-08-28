﻿using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Services.Management;

using Microsoft.Extensions.Logging;

using static ClassIsland.Shared.Helpers.ConfigureFileHelper;

using Path = System.IO.Path;
using System.Windows.Input;
using ClassIsland.Shared.IPC.Abstractions.Services;
using dotnetCampus.Ipc.CompilerServices.GeneratedProxies;
using Sentry;

namespace ClassIsland.Services;

public class ProfileService : IProfileService
{
    public string CurrentProfilePath { 
        get; 
        set;
    } = @".\Profiles\Default.json";

    public static readonly string ManagementClassPlanPath =
        Path.Combine(Management.ManagementService.ManagementConfigureFolderPath, "ClassPlans.json");

    public static readonly string ManagementTimeLayoutPath =
        Path.Combine(Management.ManagementService.ManagementConfigureFolderPath, "TimeLayouts.json");

    public static readonly string ManagementSubjectsPath =
        Path.Combine(Management.ManagementService.ManagementConfigureFolderPath, "Subjects.json");

    public Profile Profile {
        get;
        set;
    } = new Profile();

    private SettingsService SettingsService { get; }

    private ILogger<ProfileService> Logger { get; }

    private IManagementService ManagementService { get; }
    public IIpcService IpcService { get; }

    private bool _isProfileLoaded = false;

    public ProfileService(SettingsService settingsService, ILogger<ProfileService> logger, IManagementService managementService, IIpcService ipcService)
    {
        Logger = logger;
        ManagementService = managementService;
        IpcService = ipcService;
        SettingsService = settingsService;
        IpcService.IpcProvider.CreateIpcJoint<IPublicProfileService>(this);
        if (!Directory.Exists("./Profiles"))
        {
            Directory.CreateDirectory("./Profiles");
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
        var path = $"./Profiles/{filename}";
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

        CurrentProfilePath = filename;
        Logger.LogTrace("成功加载档案！");
        CleanExpiredTempClassPlan();
        _isProfileLoaded = true;
        spanLoadingProfile?.Finish();
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
        Logger.LogInformation("写入档案文件：{}", $"./Profiles/{filename}");
        SaveConfig($"./Profiles/{filename}", Profile);
    }

    private static T DuplicateJson<T>(T o)
    {
        var json = JsonSerializer.Serialize(o);
        return JsonSerializer.Deserialize<T>(json)!;
    }

    public string? CreateTempClassPlan(string id, string? timeLayoutId=null)
    {
        Logger.LogInformation("创建临时层：{}", id);
        if (Profile.OverlayClassPlanId != null && Profile.ClassPlans.ContainsKey(Profile.OverlayClassPlanId))
        {
            return null;
        }
        var cp = Profile.ClassPlans[id];
        timeLayoutId = timeLayoutId ?? cp.TimeLayoutId;
        var newCp = DuplicateJson(cp);

        newCp.IsOverlay = true;
        newCp.TimeLayoutId = timeLayoutId;
        newCp.OverlaySourceId = id;
        newCp.Name += "（临时层）";
        newCp.OverlaySetupTime = App.GetService<IExactTimeService>().GetCurrentLocalDateTime().Date;
        Profile.IsOverlayClassPlanEnabled = true;
        var newId = Guid.NewGuid().ToString();
        Profile.OverlayClassPlanId = newId;
        Profile.ClassPlans.Add(newId, newCp);
        return newId;
    }

    public void ClearTempClassPlan()
    {
        if (Profile.OverlayClassPlanId == null || !Profile.ClassPlans.ContainsKey(Profile.OverlayClassPlanId))
        {
            return;
        }

        Logger.LogInformation("清空临时层：{}", Profile.OverlayClassPlanId);
        Profile.IsOverlayClassPlanEnabled = false;
        Profile.ClassPlans.Remove(Profile.OverlayClassPlanId);
        Profile.OverlayClassPlanId = null;
    }

    public void CleanExpiredTempClassPlan()
    {
        if (Profile.OverlayClassPlanId == null || !Profile.ClassPlans.ContainsKey(Profile.OverlayClassPlanId))
        {
            return;
        }

        var cp = Profile.ClassPlans[Profile.OverlayClassPlanId];
        if (cp.OverlaySetupTime.Date < App.GetService<IExactTimeService>().GetCurrentLocalDateTime().Date)
        {
            Logger.LogInformation("清理过期的临时层课表。");
            ClearTempClassPlan();
        }
    }

    //[Obsolete]
    //public bool CheckClassPlan(ClassPlan plan)
    //{
    //}

    public void ConvertToStdClassPlan()
    {
        Logger.LogInformation("将临时层课表转换为普通课表：{}", Profile.OverlayClassPlanId);
        if (Profile.OverlayClassPlanId == null || !Profile.ClassPlans.ContainsKey(Profile.OverlayClassPlanId))
        {
            return;
        }

        Profile.IsOverlayClassPlanEnabled = false;
        Profile.ClassPlans[Profile.OverlayClassPlanId].IsOverlay = false;
        Profile.OverlayClassPlanId = null;
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
            var finalOffset = baseOffset + divOffset * 7;
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

    public void ClearExpiredTempClassPlanGroup()
    {
        if (Profile.TempClassPlanGroupExpireTime.Date < App.GetService<IExactTimeService>().GetCurrentLocalDateTime().Date)
        {
            ClearTempClassPlanGroup();
        }
    }
}