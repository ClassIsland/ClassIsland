using Octokit;
using System.Security.Policy;
using ClassIsland.Core.Abstractions.Models.Marketplace;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.XamlTheme;

/// <summary>
/// 代表主题清单信息
/// </summary>
public class ThemeManifest : ObservableRecipient, IMarketplaceItemManifest
{
    private string _name = "";
    private string _author = "";
    private string _url = "";
    private string _id = "";
    private string _description = "";
    private string _version = "0.0.0.0";
    private string _banner = "banner.png";

    /// <summary>
    /// 主题 ID
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
    /// 主题描述
    /// </summary>
    public string Description
    {
        get => _description;
        set
        {
            if (value == _description) return;
            _description = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 主题版本
    /// </summary>
    public string Version
    {
        get => _version;
        set
        {
            if (value == _version) return;
            _version = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 主题名称
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (value == _name) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 主题作者
    /// </summary>
    public string Author
    {
        get => _author;
        set
        {
            if (value == _author) return;
            _author = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 主题 URL
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
    /// 主题 Banner 路径
    /// </summary>
    public string Banner
    {
        get => _banner;
        set
        {
            if (value == _banner) return;
            _banner = value;
            OnPropertyChanged();
        }
    }
}