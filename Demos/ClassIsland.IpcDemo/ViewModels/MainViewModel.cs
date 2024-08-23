using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.IpcDemo.ViewModels;

public class MainViewModel : ObservableRecipient
{
    private string _uri = "";
    private string _lessonInfos = "";

    public string Uri
    {
        get => _uri;
        set
        {
            if (value == _uri) return;
            _uri = value;
            OnPropertyChanged();
        }
    }

    public string LessonInfos
    {
        get => _lessonInfos;
        set
        {
            if (value == _lessonInfos) return;
            _lessonInfos = value;
            OnPropertyChanged();
        }
    }
}