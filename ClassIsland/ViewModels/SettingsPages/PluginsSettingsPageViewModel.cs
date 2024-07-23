using System.Windows.Documents;
using ClassIsland.Core.Models.Plugin;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class PluginsSettingsPageViewModel : ObservableRecipient
{
    private PluginInfo? _selectedPluginInfo;
    private FlowDocument _readmeDocument = new();

    public PluginInfo? SelectedPluginInfo
    {
        get => _selectedPluginInfo;
        set
        {
            if (Equals(value, _selectedPluginInfo)) return;
            _selectedPluginInfo = value;
            OnPropertyChanged();
        }
    }

    public FlowDocument ReadmeDocument
    {
        get => _readmeDocument;
        set
        {
            if (Equals(value, _readmeDocument)) return;
            _readmeDocument = value;
            OnPropertyChanged();
        }
    }
}