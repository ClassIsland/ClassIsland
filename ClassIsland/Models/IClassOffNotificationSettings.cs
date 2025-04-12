using System.ComponentModel;

namespace ClassIsland.Models;

public interface IClassOffNotificationSettings : INotifyPropertyChanged
{
    bool IsClassOffNotificationEnabled { get; set; }
    string ClassOffMaskText { get; set; }
    string ClassOffOverlayText { get; set; }
}