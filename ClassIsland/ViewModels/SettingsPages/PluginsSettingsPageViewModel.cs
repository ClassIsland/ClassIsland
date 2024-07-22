using System.Windows.Documents;
using ClassIsland.Core.Models.Plugin;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class PluginsSettingsPageViewModel : ObservableRecipient
{
    private PluginManifest _selectedPluginManifest = new();
    private FlowDocument _readmeDocument = new();

    public PluginManifest SelectedPluginManifest
    {
        get => _selectedPluginManifest;
        set
        {
            if (Equals(value, _selectedPluginManifest)) return;
            _selectedPluginManifest = value;
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