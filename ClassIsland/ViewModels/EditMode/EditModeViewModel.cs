using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.EditMode;

public partial class EditModeViewModel(MainWindow mainWindow) : ObservableObject
{
    public MainWindow MainWindow { get; } = mainWindow;
}