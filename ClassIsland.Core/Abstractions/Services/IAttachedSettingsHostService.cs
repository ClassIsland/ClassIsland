using System.Collections.ObjectModel;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Abstractions.Services;

public interface IAttachedSettingsHostService
{
    /// <summary>
    /// 已注册的附加设置控件信息。
    /// </summary>
    public static ObservableCollection<AttachedSettingsControlInfo> RegisteredControls { get; } = new();

    public static ObservableCollection<AttachedSettingsControlInfo> TimePointSettingsAttachedSettingsControls { get; } =
        new();

    public static ObservableCollection<AttachedSettingsControlInfo>
        TimeLayoutSettingsAttachedSettingsControls { get; } = new();
    public static ObservableCollection<AttachedSettingsControlInfo> ClassPlanSettingsAttachedSettingsControls { get; } = new();
    public static ObservableCollection<AttachedSettingsControlInfo> SubjectSettingsAttachedSettingsControls { get; } = new();
    
    public static T? GetAttachedSettingsByPriority<T>(Guid id, 
        Subject? subject = null,
        TimeLayoutItem? timeLayoutItem = null, 
        ClassPlan? classPlan = null, 
        TimeLayout? timeLayout = null) 
        where T : IAttachedSettings
    {
        var l = new AttachableSettingsObject?[] { subject, timeLayoutItem, classPlan, timeLayout };
        foreach (var i in l)
        {
            if (i == null) continue;
            var o = i.GetAttachedObject<T>(id);
            if (o?.IsAttachSettingsEnabled == true)
            {
                return o;
            }
        }

        return default;
    }
}