using System.Collections.ObjectModel;
using System.Windows.Documents;

using ClassIsland.Core;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class HelpsViewModel : ObservableRecipient
{
    private FlowDocument _document = new();
    private string? _selectedDocumentName = "";
    private ObservableDictionary<string, string> _helpDocuments = new();
    private bool _isOpened = false;
    private ObservableCollection<string> _navigationHistory = new();
    private int _navigationIndex = -1;
    private bool _canForward = false;
    private bool _canBack = false;
    private bool _isLoading = true;

    public FlowDocument Document
    {
        get => _document;
        set
        {
            if (Equals(value, _document)) return;
            _document = value;
            OnPropertyChanged();
        }
    }

    public string? SelectedDocumentName
    {
        get => _selectedDocumentName;
        set
        {
            if (value == _selectedDocumentName) return;
            _selectedDocumentName = value;
            OnPropertyChanged();
        }
    }

    public ObservableDictionary<string, string> HelpDocuments
    {
        get => _helpDocuments;
        set
        {
            if (Equals(value, _helpDocuments)) return;
            _helpDocuments = value;
            OnPropertyChanged();
        }
    }

    public bool IsOpened
    {
        get => _isOpened;
        set
        {
            if (value == _isOpened) return;
            _isOpened = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> NavigationHistory
    {
        get => _navigationHistory;
        set
        {
            if (Equals(value, _navigationHistory)) return;
            _navigationHistory = value;
            OnPropertyChanged();
        }
    }

    public int NavigationIndex
    {
        get => _navigationIndex;
        set
        {
            if (value == _navigationIndex) return;
            _navigationIndex = value;
            OnPropertyChanged();
        }
    }

    public bool CanBack
    {
        get => _canBack;
        set
        {
            if (value == _canBack) return;
            _canBack = value;
            OnPropertyChanged();
        }
    }

    public bool CanForward
    {
        get => _canForward;
        set
        {
            if (value == _canForward) return;
            _canForward = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (value == _isLoading) return;
            _isLoading = value;
            OnPropertyChanged();
        }
    }
}