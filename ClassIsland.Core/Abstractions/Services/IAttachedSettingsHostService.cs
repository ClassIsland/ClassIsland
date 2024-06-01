using System.Collections.ObjectModel;
using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Abstractions.Services;

public interface IAttachedSettingsHostService
{
    ObservableCollection<Type> TimePointSettingsAttachedSettingsControls { get; }
    ObservableCollection<Type> TimeLayoutSettingsAttachedSettingsControls { get; }
    ObservableCollection<Type> ClassPlanSettingsAttachedSettingsControls { get; }
    ObservableCollection<Type> SubjectSettingsAttachedSettingsControls { get; }
    
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