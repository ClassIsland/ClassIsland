using System.IO;
using ClassIsland.Core.Abstractions.Models.Marketplace;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace ClassIsland.Core.Models.XamlTheme;

/// <summary>
/// 代表主题信息
/// </summary>
public class ThemeInfo : ObservableRecipient, IMarketplaceItemInfo
{
    private bool _isLoaded = false;
    private bool _isError = false;
    private Exception? _error;
    private string _path = "";
    private ThemeManifest _manifest = new();
    private bool _isAvailableOnMarket = false;

    /// <summary>
    /// 主题清单
    /// </summary>
    public ThemeManifest Manifest
    {
        get => _manifest;
        set
        {
            if (Equals(value, _manifest)) return;
            _manifest = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ManifestReadonly));
        }
    }

    /// <summary>
    /// 主题安装路径
    /// </summary>

    [YamlIgnore]
    public string Path
    {
        get => _path;
        set
        {
            if (value == _path) return;
            _path = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 主题是否已加载
    /// </summary>
    [YamlIgnore]
    public bool IsLoaded
    {
        get => _isLoaded;
        set
        {
            if (value == _isLoaded) return;
            _isLoaded = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 主题是否出错
    /// </summary>
    [YamlIgnore]
    public bool IsError
    {
        get => _isError;
        set
        {
            if (value == _isError) return;
            _isError = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 主题错误信息
    /// </summary>
    [YamlIgnore]
    public Exception? Error
    {
        get => _error;
        set
        {
            if (Equals(value, _error)) return;
            _error = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 主题是否已启用
    /// </summary>
    [YamlIgnore]
    public bool IsEnabled
    {
        get => File.Exists(System.IO.Path.Combine(Path, ".enabled"));
        set
        {
            try
            {
                if (value)
                {
                    File.WriteAllText(System.IO.Path.Combine(Path, ".enabled"), "");
                }
                else
                {
                    File.Delete(System.IO.Path.Combine(Path, ".enabled"));
                }
            }
            catch (Exception e)
            {
                // ignored
            }
        }
    }

    /// <inheritdoc />
    [JsonIgnore] public IMarketplaceItemManifest ManifestReadonly => Manifest;

    /// <summary>
    /// 主题是否在市场上可用
    /// </summary>
    [JsonIgnore]
    public bool IsAvailableOnMarket
    {
        get => _isAvailableOnMarket;
        set
        {
            if (value == _isAvailableOnMarket) return;
            _isAvailableOnMarket = value;
            OnPropertyChanged();
        }
    }
}