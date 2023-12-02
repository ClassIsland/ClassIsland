using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class ExcelImportViewModel : ObservableRecipient
{
    private int _slideIndex = 0;
    private int _descriptionMode = 0;
    private bool _isLoadingExcelFile = false;
    private bool _isFileSelected = false;
    private bool _isSelectingMode = false;

    public int SlideIndex
    {
        get => _slideIndex;
        set
        {
            if (value == _slideIndex) return;
            _slideIndex = value;
            OnPropertyChanged();
        }
    }

    public int DescriptionMode
    {
        get => _descriptionMode;
        set
        {
            if (value == _descriptionMode) return;
            _descriptionMode = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoadingExcelFile
    {
        get => _isLoadingExcelFile;
        set
        {
            if (value == _isLoadingExcelFile) return;
            _isLoadingExcelFile = value;
            OnPropertyChanged();
        }
    }

    public bool IsFileSelected
    {
        get => _isFileSelected;
        set
        {
            if (value == _isFileSelected) return;
            _isFileSelected = value;
            OnPropertyChanged();
        }
    }

    public bool IsSelectingMode
    {
        get => _isSelectingMode;
        set
        {
            if (value == _isSelectingMode) return;
            _isSelectingMode = value;
            OnPropertyChanged();
        }
    }
}