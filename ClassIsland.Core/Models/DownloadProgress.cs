using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models;

/// <summary>
/// 代表下载进度
/// </summary>
public class DownloadProgress : ObservableRecipient
{
    private double _progress = 0.0;
    private CancellationToken _cancellationToken;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private bool _isDownloading = false;
    private Exception? _exception;

    /// <inheritdoc />
    public DownloadProgress()
    {
        _cancellationToken = _cancellationTokenSource.Token;
    }

    /// <summary>
    /// 下载进度
    /// </summary>
    public double Progress
    {
        get => _progress;
        set
        {
            if (value.Equals(_progress)) return;
            _progress = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 下载操作取消令牌
    /// </summary>
    public CancellationToken CancellationToken
    {
        get => _cancellationToken;
        set
        {
            if (value.Equals(_cancellationToken)) return;
            _cancellationToken = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否正在下载
    /// </summary>
    public bool IsDownloading
    {
        get => _isDownloading;
        set
        {
            if (value == _isDownloading) return;
            _isDownloading = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 下载时产生的异常
    /// </summary>
    public Exception? Exception
    {
        get => _exception;
        set
        {
            if (Equals(value, _exception)) return;
            _exception = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 取消下载。
    /// </summary>
    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }
}