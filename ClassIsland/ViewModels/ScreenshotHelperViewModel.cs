using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Controls;
using ClassIsland.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class ScreenshotHelperViewModel : ObservableObject
{
    [ObservableProperty] private WindowInfo? _selectedWindow;
    
    [ObservableProperty] private ObservableCollection<WindowInfo>? _windows;
    
    [ObservableProperty] private double _scale = 1.0;
    
    [ObservableProperty] private string _imageBasePath = Path.Combine(CommonDirectories.AppTempFolderPath, "Screenshots");
    
    public class WindowInfo(Window window)
    {
        public Window Window { get; } = window;

        public int Id { get; } = window.GetHashCode();
    }
}