using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Updating;

public class VersionsIndex : ObservableRecipient
{
    // 注意：考虑到给这些值添加属性更变通知会导致设置页面上的【更新通道】和【更新镜像源】选项在检查更新后出现问题（变成空值），
    //      不要给这个类型中的属性启用属性更变通知。
    public Dictionary<string, DownloadMirror> Mirrors
    {
        get;
        set;
    } = new();

    public List<VersionInfoMin> Versions
    {
        get;
        set;
    } = new();

    public Dictionary<string, ChannelInfo> Channels
    {
        get;
        set;
    } = new();
}