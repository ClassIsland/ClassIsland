using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using ClassIsland.Core.Controls;
using ClassIsland.Enums;

namespace ClassIsland.Views;

public partial class DataTransferWindow : MyWindow
{
    public int ImportStage { init; get; } = -1;
    
    public string? ImportName { get; init; }

    private DataTransferPage _dataTransferPage;
    
    
    public DataTransferWindow()
    {
        InitializeComponent();
        Content = _dataTransferPage = new DataTransferPage();
        if (OperatingSystem.IsMacOS())
        {
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;
            ExtendClientAreaTitleBarHeightHint = -1;
            SystemDecorations = SystemDecorations.Full;
        }
    }

    public async Task PerformClassIslandImport(string root, ImportEntries importEntries)
    {
#pragma warning disable CS0612 // 类型或成员已过时
        await _dataTransferPage.PerformClassIslandImport(root, importEntries);
#pragma warning restore CS0612 // 类型或成员已过时
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        
    }

    public void ImportComplete()
    {
        _dataTransferPage.ViewModel.PageIndex = 4;
    }
}