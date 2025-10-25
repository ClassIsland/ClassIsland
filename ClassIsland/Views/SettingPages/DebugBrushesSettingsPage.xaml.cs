#if false
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// DebugBrushesSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("debug_brushes", "笔刷", MaterialIconKind.BrushOutline, MaterialIconKind.Brush, SettingsPageCategory.Debug)]
public partial class DebugBrushesSettingsPage : SettingsPageBase
{
    public DebugBrushesSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
    }
}
#endif
