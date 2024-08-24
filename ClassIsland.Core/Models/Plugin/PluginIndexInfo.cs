using System.Text.Json.Serialization;
using ClassIsland.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Plugin;

/// <summary>
/// 插件索引信息
/// </summary>
public class PluginIndexInfo : ObservableRecipient
{
    private string _id = Guid.NewGuid().ToString();
    private string _url = "";
    private string _selectedMirror = "";
    private ObservableDictionary<string, string> _mirrors = new();

    /// <summary>
    /// 插件索引 Guid
    /// </summary>
    public string Id
    {
        get => _id;
        set
        {
            if (value == _id) return;
            _id = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 插件索引 Url
    /// </summary>
    public string Url
    {
        get => _url;
        set
        {
            if (value == _url) return;
            _url = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 已选择的镜像
    /// </summary>
    public string SelectedMirror
    {
        get => _selectedMirror;
        set
        {
            if (value == _selectedMirror) return;
            _selectedMirror = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 此插件源的镜像列表
    /// </summary>
    [JsonIgnore]
    public ObservableDictionary<string, string> Mirrors
    {
        get => _mirrors;
        set
        {
            if (Equals(value, _mirrors)) return;
            _mirrors = value;
            OnPropertyChanged();
        }
    }
}