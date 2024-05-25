using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

using ClassIsland.Core.Models.Profile;
using ClassIsland.Services.Management;

using Microsoft.Extensions.Logging;

using static ClassIsland.Core.Helpers.ConfigureFileHelper;

using Path = System.IO.Path;

namespace ClassIsland.Services;

public class ProfileService
{
    public string CurrentProfilePath { 
        get; 
        set;
    } = @".\Profiles\Default.json";

    public static readonly string ManagementClassPlanPath =
        Path.Combine(ManagementService.ManagementConfigureFolderPath, "ClassPlans.json");

    public static readonly string ManagementTimeLayoutPath =
        Path.Combine(ManagementService.ManagementConfigureFolderPath, "TimeLayouts.json");

    public static readonly string ManagementSubjectsPath =
        Path.Combine(ManagementService.ManagementConfigureFolderPath, "Subjects.json");

    public Profile Profile {
        get;
        set;
    } = new Profile();

    private SettingsService SettingsService { get; }

    private ILogger<ProfileService> Logger { get; }

    private ManagementService ManagementService { get; }

    public ProfileService(SettingsService settingsService, ILogger<ProfileService> logger, ManagementService managementService)
    {
        Logger = logger;
        ManagementService = managementService;
        SettingsService = settingsService;
        if (!Directory.Exists("./Profiles"))
        {
            Directory.CreateDirectory("./Profiles");
        }
    }


    private async Task MergeManagementProfileAsync()
    {
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
                var cpOld = LoadConfig<Profile>(ManagementClassPlanPath);
                var cpNew = classPlan = await ManagementService.Connection.GetJsonAsync<Profile>(ManagementService.Manifest.ClassPlanSource.Value!);
                MergeDictionary(Profile.ClassPlans, cpOld.ClassPlans, cpNew.ClassPlans);
            }
            if (ManagementService.Manifest.TimeLayoutSource.IsNewerAndNotNull(ManagementService.Versions.TimeLayoutVersion))
            {
                var tlOld = LoadConfig<Profile>(ManagementTimeLayoutPath);
                var tlNew = timeLayouts = await ManagementService.Connection.GetJsonAsync<Profile>(ManagementService.Manifest.TimeLayoutSource.Value!);
                MergeDictionary(Profile.TimeLayouts, tlOld.TimeLayouts, tlNew.TimeLayouts);
            }
            if (ManagementService.Manifest.SubjectsSource.IsNewerAndNotNull(ManagementService.Versions.SubjectsVersion))
            {
                var subjectOld = LoadConfig<Profile>(ManagementSubjectsPath);
                var subjectNew = subjects = await ManagementService.Connection.GetJsonAsync<Profile>(ManagementService.Manifest.SubjectsSource.Value!);
                MergeDictionary(Profile.Subjects, subjectOld.Subjects, subjectNew.Subjects);
            }

            SaveProfile("_management-profile.json");
            ManagementService.Versions.ClassPlanVersion = ManagementService.Manifest.ClassPlanSource.Version;
            ManagementService.Versions.TimeLayoutVersion = ManagementService.Manifest.TimeLayoutSource.Version;
            ManagementService.Versions.SubjectsVersion = ManagementService.Manifest.SubjectsSource.Version;
            ManagementService.SaveSettings();
        }
        catch (Exception exp)
        {
            Logger.LogError(exp, "拉取档案失败。");
        }

        //Profile = ConfigureFileHelper.CopyObject(Profile);
        Profile.Subjects = CopyObject(Profile.Subjects);
        Profile.TimeLayouts = CopyObject(Profile.TimeLayouts);
        Profile.ClassPlans = CopyObject(Profile.ClassPlans);
        Profile.RefreshTimeLayouts();
        Logger.LogTrace("成功拉取集控档案！");
    }

    public async Task LoadProfileAsync()
    {
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

        var json = await File.ReadAllTextAsync(path);
        var r = JsonSerializer.Deserialize<Profile>(json);
        if (r != null)
        {
            Profile = r;
            if (ManagementService.IsManagementEnabled)
                await MergeManagementProfileAsync();
            Profile.PropertyChanged += (sender, args) => SaveProfile(filename);
        }

        CurrentProfilePath = filename;
        Logger.LogTrace("成功加载档案！");
        CleanExpiredTempClassPlan();
    }

    public void SaveProfile()
    {
        SaveProfile(CurrentProfilePath);
    }

    public void SaveProfile(string filename)
    {
        Logger.LogInformation("写入档案文件：{}", $"./Profiles/{filename}");
        var json = JsonSerializer.Serialize<Profile>(Profile);
        //File.WriteAllText("./Profile.json", json);
        File.WriteAllText($"./Profiles/{filename}", json);
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
        newCp.OverlaySetupTime = DateTime.Now;
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
        if (cp.OverlaySetupTime.Date < DateTime.Now.Date)
        {
            Logger.LogInformation("清理过期的临时层课表。");
            ClearTempClassPlan();
        }
    }

    public bool CheckClassPlan(ClassPlan plan)
    {
        if (plan.TimeRule.WeekDay != (int)DateTime.Now.DayOfWeek)
        {
            return false;
        }

        var dd = DateTime.Now.Date - SettingsService.Settings.SingleWeekStartTime.Date;
        var dw = Math.Floor(dd.TotalDays / 7) + 1;
        var w = (int)dw % 2;
        switch (plan.TimeRule.WeekCountDiv)
        {
            case 1 when w != 1:
                return false;
            case 2 when w != 0:
                return false;
            default:
                return true;
        }
    }

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
}