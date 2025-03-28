using System.ComponentModel;

namespace ClassIsland.Models;

public interface IClassNotificationSettings : INotifyPropertyChanged
{
    bool IsClassOnNotificationEnabled { get; set; }
    bool IsClassOnPreparingNotificationEnabled { get; set; }
    bool IsClassOffNotificationEnabled { get; set; }
    string ClassOnPreparingText { get; set; }
    string OutdoorClassOnPreparingText { get; set; }


    string ClassOnPreparingMaskText { get; set; }
    string ClassOnMaskText { get; set; }
    string ClassOffMaskText { get; set; }

    string ClassOffOverlayText { get; set; }
}