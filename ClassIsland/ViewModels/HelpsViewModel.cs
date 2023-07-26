using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class HelpsViewModel : ObservableRecipient
{
    private FlowDocument _document = new();
    private string? _selectedDocumentName = "";
    private ObservableDictionary<string, string> _helpDocuments = new();
    private bool _isOpened = false;

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
}