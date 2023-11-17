using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class ExcelImportViewModel : ObservableRecipient
{
    private int _slideIndex = 0;

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
}