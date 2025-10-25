using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.ComponentSettings;

namespace ClassIsland.Controls.Components;

/// <summary>
/// TestComponent.xaml 的交互逻辑
/// </summary>
[ComponentInfo("EE8F66BD-C423-4E7C-AB46-AA9976B00E08", "文本", "\uf323", "显示自定义文本。")]
public partial class TextComponent : ComponentBase<TextComponentSettings>
{
    public TextComponent()
    {
        InitializeComponent();
    }
}
