using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ClassIsland.Models;

namespace ClassIsland.Services;

public class ProfileService
{
    public Profile Profile { get; set; } = new Profile();

    private SettingsService SettingsService { get; }

    public ProfileService(SettingsService settingsService)
    {
        SettingsService = settingsService;
        LoadProfile();
        CleanExpiredTempClassPlan();
    }

    public void LoadProfile()
    {
        var path = $"./Profiles/{SettingsService.Settings.SelectedProfile}";
        if (!File.Exists(path))
        {
            SaveProfile();
        }

        var json = File.ReadAllText(path);
        var r = JsonSerializer.Deserialize<Profile>(json);
        if (r != null)
        {
            Profile = r;
            Profile.PropertyChanged += (sender, args) => SaveProfile();
        }
    }

    public void SaveProfile()
    {
        var json = JsonSerializer.Serialize<Profile>(Profile);
        //File.WriteAllText("./Profile.json", json);
        File.WriteAllText($"./Profiles/{SettingsService.Settings.SelectedProfile}", json);
    }

    private static T DuplicateJson<T>(T o)
    {
        var json = JsonSerializer.Serialize(o);
        return JsonSerializer.Deserialize<T>(json)!;
    }

    public string? CreateTempClassPlan(string id)
    {
        if (Profile.OverlayClassPlanId != null && Profile.ClassPlans.ContainsKey(Profile.OverlayClassPlanId))
        {
            return null;
        }
        var cp = Profile.ClassPlans[id];
        var newCp = DuplicateJson(cp);

        newCp.IsOverlay = true;
        newCp.OverlaySourceId = id;
        newCp.Name += "（副本）";
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

        if (!CheckClassPlan(Profile.ClassPlans[Profile.OverlayClassPlanId]))
        {
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
        if (Profile.OverlayClassPlanId == null || !Profile.ClassPlans.ContainsKey(Profile.OverlayClassPlanId))
        {
            return;
        }

        Profile.IsOverlayClassPlanEnabled = false;
        Profile.ClassPlans[Profile.OverlayClassPlanId].IsOverlay = false;
        Profile.OverlayClassPlanId = null;
    }
}