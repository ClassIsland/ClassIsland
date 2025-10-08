namespace ClassIsland.Platforms.Windows.FilePickerHost.Models;

public class FilePickerHostArguments
{
    public FilePickerMode Mode { get; set; } = FilePickerMode.None;
    
    public object? Options { get; set; }
    
    public enum FilePickerMode
    {
        None,
        OpenFile,
        OpenFolder,
        SaveFile
    }
}