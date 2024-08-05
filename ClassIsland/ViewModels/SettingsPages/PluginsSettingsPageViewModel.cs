using System.Windows.Documents;
using ClassIsland.Core.Models.Plugin;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class PluginsSettingsPageViewModel : ObservableRecipient
{
    private PluginInfo? _selectedPluginInfo;
    private FlowDocument _readmeDocument = new();
    private bool _isPluginOperationsPopupOpened = false;
    private bool _isPluginMarketOperationsPopupOpened = false;
    private PluginIndexInfo? _selectedPluginIndexInfo;
    private int _pluginCategoryIndex = 1;
    private string _pluginFilterText = "";

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

    public bool IsPluginOperationsPopupOpened
    {
        get => _isPluginOperationsPopupOpened;
        set
        {
            if (value == _isPluginOperationsPopupOpened) return;
            _isPluginOperationsPopupOpened = value;
            OnPropertyChanged();
        }
    }

    public bool IsPluginMarketOperationsPopupOpened
    {
        get => _isPluginMarketOperationsPopupOpened;
        set
        {
            if (value == _isPluginMarketOperationsPopupOpened) return;
            _isPluginMarketOperationsPopupOpened = value;
            OnPropertyChanged();
        }
    }

    public PluginIndexInfo? SelectedPluginIndexInfo
    {
        get => _selectedPluginIndexInfo;
        set
        {
            if (Equals(value, _selectedPluginIndexInfo)) return;
            _selectedPluginIndexInfo = value;
            OnPropertyChanged();
        }
    }

    public int PluginCategoryIndex
    {
        get => _pluginCategoryIndex;
        set
        {
            if (value == _pluginCategoryIndex) return;
            _pluginCategoryIndex = value;
            OnPropertyChanged();
        }
    }

    public string PluginFilterText
    {
        get => _pluginFilterText;
        set
        {
            if (value == _pluginFilterText) return;
            _pluginFilterText = value;
            OnPropertyChanged();
        }
    }
}