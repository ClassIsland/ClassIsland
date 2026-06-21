using System.Collections.Generic;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.AllContributors;

internal class AllContributorsRc : ObservableRecipient
{
    private List<Contributor> _contributors = new();

    public List<Contributor> Contributors
    {
        get => _contributors;
        set
        {
            if (Equals(value, _contributors)) return;
            _contributors = value;
            OnPropertyChanged();
        }
    }
}