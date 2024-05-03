using System.ComponentModel;

namespace ClassIsland.Core.Interfaces;

public interface ITimeRule : INotifyPropertyChanged
{
    public string Name
    {
        get;
        set;
    }

    public bool IsSatisfied
    {
        get; set;
    }
}