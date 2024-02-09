using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using ClassIsland.Models;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class ProfileService
{
    public string CurrentProfilePath { get; set; } = @".\Profiles\Default.json";
    
    public Profile Profile { get; set; } = new Profile();

    private SettingsService SettingsService { get; }

    private ILogger<ProfileService> Logger { get; }

    public ProfileService(SettingsService settingsService, ILogger<ProfileService> logger)
    {
        Logger = logger;
        SettingsService = settingsService;
        if (!Directory.Exists("./Profiles"))
        {
            Directory.CreateDirectory("./Profiles");
        }
        LoadProfile();
        CleanExpiredTempClassPlan();
    }

    public void LoadProfile()
    {
        var filename = SettingsService.Settings.SelectedProfile;
        var path = $"./Profiles/{filename}";
        Logger.LogInformation("加载档案中：{}", path);
        if (!File.Exists(path))
        {
            Logger.LogInformation("档案不存在：{}", path);
            var subject = new StreamReader(Application.GetResourceStream(new Uri("/Assets/default-subjects.json", UriKind.Relative))!.Stream).ReadToEnd();
            Profile.Subjects = JsonSerializer.Deserialize<Profile>(subject)!.Subjects;
            SaveProfile(filename);
        }

        var json = File.ReadAllText(path);
        var r = JsonSerializer.Deserialize<Profile>(json);
        if (r != null)
        {
            Profile = r;
            Profile.PropertyChanged += (sender, args) => SaveProfile(filename);
        }

        CurrentProfilePath = filename;
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

    public string? CreateTempClassPlan(string id)
    {
        Logger.LogInformation("创建临时层：{}", id);
        if (Profile.OverlayClassPlanId != null && Profile.ClassPlans.ContainsKey(Profile.OverlayClassPlanId))
        {
            return null;
        }
        var cp = Profile.ClassPlans[id];
        var newCp = DuplicateJson(cp);

        newCp.IsOverlay = true;
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