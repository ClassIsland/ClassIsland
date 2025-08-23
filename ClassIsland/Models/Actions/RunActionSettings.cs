using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
namespace ClassIsland.Models.Actions;

/// <summary>
/// "运行"行动设置。
/// </summary>
public partial class RunActionSettings : ObservableRecipient
{
    [ObservableProperty]
    RunActionRunType _runType;

    [ObservableProperty]
    string _value = "";

    [ObservableProperty]
    string _args = "";

    /// <summary>
    /// "运行"行动运行类型。
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RunActionRunType
    {
        /// <summary>
        /// 应用程序
        /// </summary>
        Application,

        /// <summary>
        /// 命令
        /// </summary>
        Command,

        /// <summary>
        /// 文件
        /// </summary>
        File,

        /// <summary>
        /// 文件夹
        /// </summary>
        Folder,

        /// <summary>
        /// Url 链接
        /// </summary>
        Url
    }
}