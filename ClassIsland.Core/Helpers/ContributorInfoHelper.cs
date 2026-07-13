using System.Diagnostics.Contracts;
using System.Reflection;
using System.Text;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Plugin;
namespace ClassIsland.Core.Helpers;

/// ContributorInfo 注册信息提取工具。
public static class ContributorInfoHelper
{
    /// RegisterPlugin 会由 PluginService 在加载插件时调用，注册 Assembly 对应的插件名称。
    internal static void RegisterPlugin(Assembly assembly, PluginManifest manifest)
    {
        var info = new ContributorInfo
        {
            PluginName = manifest.Name,
            PluginMessage = BuildDefaultMessage()
        };
        IPluginService.PluginContributorInfos.Add(assembly, info);
        return;

        string BuildDefaultMessage()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(manifest.Name))
                sb.AppendLine($"插件  **{manifest.Name}**");
            if (!string.IsNullOrEmpty(manifest.Author))
                sb.AppendLine($"作者  @{manifest.Author}");
            sb.Append($"项目主页  [插件详情页](classisland://app/settings/classisland.plugins?pluginId={manifest.Id}&ci_keepHistory=true) ");
            if (!string.IsNullOrEmpty(manifest.Url))
                sb.Append($"[在浏览器中打开↗]({manifest.Url})");
            sb.AppendLine();
            sb.AppendLine();
            sb.Append("如需获取帮助或反馈问题，请访问项目主页。");
            return sb.ToString();
        }
    }
    
    /// RegisterPluginContributorInfo 由插件使用 IServiceCollection 方法调用，为插件注册全局 ContributorInfo 信息。
    internal static void RegisterPluginContributorInfo(Assembly assembly, string? message = null, string? name = null) {
        ContributorInfo? info;
        if (IPluginService.PluginContributorInfos.TryGetValue(assembly, out info)) {
            if (name != null) {
                info.PluginName = name;
            }
            if (message != null) {
                info.PluginMessage = message;
            }
        } else {
            throw new InvalidOperationException("插件程序集未在插件加载前被 PluginService 注册。");
        }
    }
    
    /// Extract 从指定类型中提取 ContributorInfo 特性，并附加插件信息。
    [Pure]
    public static ContributorInfo? Extract(Type type) {
        object[] attributes = type.GetCustomAttributes(false);
        ContributorInfo? info = null;
        foreach (object t in attributes) {
            if (t is ContributorInfo contributorInfo) {
                info = contributorInfo;
                break;
            }
        }

        ContributorInfo? pluginInfo;
        if (IPluginService.PluginContributorInfos.TryGetValue(type.Assembly, out pluginInfo)) {
            if (info == null) {
                info = new ContributorInfo();
            }
            if (info.PluginName == null) {
                info.PluginName = pluginInfo.PluginName;
            }
            if (info.PluginMessage == null) {
                info.PluginMessage = pluginInfo.PluginMessage;
            }
        }
        return info;
    }
}
