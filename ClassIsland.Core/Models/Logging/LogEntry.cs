using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Core.Models.Logging;

public class LogEntry : ObservableRecipient
{
    private DateTime _time = DateTime.Now;
    private LogLevel _logLevel = LogLevel.None;
    private string _message = "";
    private string _categoryName = "";
    private Exception? _exception;

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

    public override string ToString()
    {
        return $"[{Time}] [{LogLevel}] {CategoryName}:\n{Message}";
    }
}