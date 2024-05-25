using System.Collections.ObjectModel;
using System.Windows;

using ClassIsland.Models.Logging;

namespace ClassIsland.Services.Logging;

public class AppLogService
{
    public static readonly int MaxLogEntries = 1000;

    public ObservableCollection<LogEntry> Logs { get; } = new();

    public void AddLog(LogEntry log)
    {
        var dispatcher = Application.Current.Dispatcher;
        _ = dispatcher.InvokeAsync(() =>
        {
            Logs.Add(log);
            while (Logs.Count > MaxLogEntries)
            {
                Logs.RemoveAt(0);
            }
        });
    }
}