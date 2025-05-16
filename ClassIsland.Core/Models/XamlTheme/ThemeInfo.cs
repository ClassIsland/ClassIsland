using System.IO;
using System.Text.Json.Serialization;
using ClassIsland.Core.Abstractions.Models.Marketplace;
using CommunityToolkit.Mvvm.ComponentModel;
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
    private bool _isLocal = false;
    private DownloadProgress? _downloadProgress;
    private string _realBannerPath = "";
    private long _downloadCount = 0;
    private long _starsCount = 0;
    private bool _isUpdateAvailable = false;
    private bool _restartRequired = false;

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

    [JsonIgnore]
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
    [JsonIgnore]
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
    [JsonIgnore]
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
    [JsonIgnore]
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
    [JsonIgnore]
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

    /// <summary>
    /// 主题是否存在于本地
    /// </summary>
    [JsonIgnore]
    public bool IsLocal
    {
        get => _isLocal;
        internal set
        {
            if (value == _isLocal) return;
            _isLocal = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 关联的下载进度
    /// </summary>
    [JsonIgnore]
    public DownloadProgress? DownloadProgress
    {
        get => _downloadProgress;
        set
        {
            if (Equals(value, _downloadProgress)) return;
            _downloadProgress = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Banner 真实路径
    /// </summary>
    public string RealBannerPath
    {
        get => _realBannerPath;
        set
        {
            if (value == _realBannerPath) return;
            _realBannerPath = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// 主题下载量
    /// </summary>
    public long DownloadCount
    {
        get => _downloadCount;
        set
        {
            if (value == _downloadCount) return;
            _downloadCount = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 主题 Stars 数量
    /// </summary>
    public long StarsCount
    {
        get => _starsCount;
        set
        {
            if (value == _starsCount) return;
            _starsCount = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 更新可用
    /// </summary>
    public bool IsUpdateAvailable
    {
        get => _isUpdateAvailable;
        set
        {
            if (value == _isUpdateAvailable) return;
            _isUpdateAvailable = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否需要重启
    /// </summary>
    public bool RestartRequired
    {
        get => _restartRequired;
        set
        {
            if (value == _restartRequired) return;
            _restartRequired = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 主题是否将要卸载
    /// </summary>
    [JsonIgnore]
    public bool IsUninstalling
    {
        get
        {
            if (!IsLocal)
                return false;
            return File.Exists(System.IO.Path.Combine(Path, ".uninstall"));
        }
        set
        {
            if (!IsLocal)
                throw new InvalidOperationException("无法为不存在本地的插件设置将要卸载状态。");
            var path = System.IO.Path.Combine(Path, ".uninstall");
            if (value)
            {
                File.WriteAllText(path, "");
            }
            else
            {
                File.Delete(path);
            }
            OnPropertyChanged();
        }
    }
}