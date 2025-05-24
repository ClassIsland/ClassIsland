using System.Collections.ObjectModel;
using ClassIsland.Core.Models.Logging;

namespace ClassIsland.Core.Abstractions.Services.Logging;

/// <summary>
/// 应用日志服务
/// </summary>
public interface IAppLogService
{
    /// <summary>
    /// 已记录的日志
    /// </summary>
    ObservableCollection<LogEntry> Logs { get; }
    internal void AddLog(LogEntry log);
}