using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Automation.Triggers;

public class CronTriggerSettings : ObservableRecipient
{
    private string _cronExpression = "* * * * *";

    public string CronExpression
    {
        get => _cronExpression;
        set
        {
            if (value == _cronExpression) return;
            _cronExpression = value;
            OnPropertyChanged();
        }
    }
}