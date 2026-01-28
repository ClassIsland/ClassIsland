using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Core.Models.Logging;

/// <summary>
/// 日志条目模型
/// </summary>
public class LogEntry : ObservableRecipient
{
    private DateTime _time = DateTime.Now;
    private LogLevel _logLevel = LogLevel.None;
    private string _message = "";
    private string _categoryName = "";
    private Exception? _exception;

    /// <summary>
    /// 日志记录时间
    /// </summary>
    public DateTime Time
    {
        get => _time;
        set
        {
            if (value.Equals(_time)) return;
            _time = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 日志级别
    /// </summary>
    public LogLevel LogLevel
    {
        get => _logLevel;
        set
        {
            if (value == _logLevel) return;
            _logLevel = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 日志消息
    /// </summary>
    public string Message
    {
        get => _message;
        set
        {
            if (value == _message) return;
            _message = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 日志类别名称
    /// </summary>
    public string CategoryName
    {
        get => _categoryName;
        set
        {
            if (value == _categoryName) return;
            _categoryName = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 日志关联的异常信息
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
    /// 将日志条目转换为以下字符串表示形式:[时间] [日志级别] 分类名称:换行符 消息
    /// </summary>
    public override string ToString()
    {
        return $"[{Time}] [{LogLevel}] {CategoryName}:" + Environment.NewLine + $"{Message}";
    }
}