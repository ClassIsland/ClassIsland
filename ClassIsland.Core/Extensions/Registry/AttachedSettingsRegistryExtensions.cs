using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Services.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 附加设置控件注册的<see cref="IServiceCollection"/>扩展。
/// </summary>
public static class AttachedSettingsRegistryExtensions
{
    /// <summary>
    /// 注册附加设置控件。
    /// </summary>
    /// <typeparam name="T">要注册的附加设置控件类型。</typeparam>
    /// <param name="services"><see cref="IServiceCollection"/>对象。</param>
    /// <returns><see cref="IServiceCollection"/>对象。</returns>
    /// <exception cref="InvalidOperationException">如果注册的控件没有添加<see cref="AttachedSettingsControlInfo"/>和<see cref="AttachedSettingsUsage"/>属性，或此附加设置控件的 GUID 已经被占用，则会抛出此异常。</exception>
    public static IServiceCollection AddAttachedSettingsControl<T>(this IServiceCollection services) where T : AttachedSettingsControlBase
    {
        var type = typeof(T);
        if (type.GetCustomAttributes(false).FirstOrDefault(x => x is AttachedSettingsControlInfo) is not AttachedSettingsControlInfo info)
        {
            throw new InvalidOperationException($"无法注册附加设置控件 {type.FullName}，因为此控件有注册信息。");
        }
        if (type.GetCustomAttributes(false).FirstOrDefault(x => x is AttachedSettingsUsage) is not AttachedSettingsUsage usages)
        {
            throw new InvalidOperationException($"无法注册附加设置控件 {type.FullName}，因为此控件没有用法信息。");
        }

        if (IAttachedSettingsHostService.RegisteredControls.FirstOrDefault(x => x.Guid == info.Guid) != null)
        {
            throw new InvalidOperationException($"此附加设置控件id {info.Guid} 已经被占用。");
        }
        services.AddKeyedTransient<AttachedSettingsControlBase, T>(info.Guid);
        info.AttachedSettingsControlType = typeof(T);
        IAttachedSettingsHostService.RegisteredControls.Add(info);

        if (usages.Targets.HasFlag(AttachedSettingsTargets.Lesson))
        {
            // WIP
        }
        if (usages.Targets.HasFlag(AttachedSettingsTargets.Subject))
        {
            IAttachedSettingsHostService.SubjectSettingsAttachedSettingsControls.Add(info);
        }
        if (usages.Targets.HasFlag(AttachedSettingsTargets.TimePoint))
        {
            IAttachedSettingsHostService.TimePointSettingsAttachedSettingsControls.Add(info);
        }
        if (usages.Targets.HasFlag(AttachedSettingsTargets.ClassPlan))
        {
            IAttachedSettingsHostService.ClassPlanSettingsAttachedSettingsControls.Add(info);
        }
        if (usages.Targets.HasFlag(AttachedSettingsTargets.TimeLayout))
        {
            IAttachedSettingsHostService.TimeLayoutSettingsAttachedSettingsControls.Add(info);
        }

        return services;
    }
}