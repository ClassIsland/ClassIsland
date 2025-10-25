using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.ComponentSettings;

namespace ClassIsland.Controls.Components;

[ContainerComponent]
[ComponentInfo("2D849ECE-9F21-4C78-9434-415CFC283294", "堆叠容器", "\uf04f", "将多个容器堆叠显示。")]
public partial class StackComponent : ComponentBase<StackComponentSettings>
{
    public StackComponent()
    {
        InitializeComponent();
    }
}