using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Media;
using ClassIsland.Core;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class AppearanceSettingsViewModel(SettingsService settingsService) : ObservableRecipient
{
    [ObservableProperty] private string _fontSizeTestText = "风带来故事的种子，时间使之发芽。The quick brown fox jumps over a lazy dog.";
    
    public ObservableCollection<FontFamily> FontFamilies { get; } =
        new([..FontManager.Current.SystemFonts, MainWindow.DefaultFontFamily]);

    public List<ComboBoxOption> ThemeOptions { get; } =
    [
        new("\uE5CB", "跟随系统"),
        new("\uF465", "明亮"),
        new("\uF44B", "黑暗")
    ];

    public List<ComboBoxOption> ColorSourceOptions { get; } =
    [
        new("\uED39", "自定义"),
        new("\uF42D", "系统壁纸", false, false),
        new("\uEA1D", "系统"),
        new("\uEEED", "屏幕主题色", false, false)
    ];
    
    public SettingsService SettingsService { get; } = settingsService;
}

public sealed record ComboBoxOption(string Glyph, string Text, bool IsVisible = true, bool IsEnabled = true);
