using System.ComponentModel;

namespace ClassIsland.Models;

public interface IClassOnNotificationSettings : INotifyPropertyChanged
{
    bool IsClassOnNotificationEnabled { get; set; }
    string ClassOnMaskText { get; set; }
}