using System.ComponentModel;

namespace ClassIsland.Models;

public interface IClassPreparingNotificationSettings : INotifyPropertyChanged
{
    bool IsClassOnPreparingNotificationEnabled { get; set; }
    string ClassOnPreparingText { get; set; }
    string OutdoorClassOnPreparingText { get; set; }
    string ClassOnPreparingMaskText { get; set; }
}