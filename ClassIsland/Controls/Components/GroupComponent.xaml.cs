using ClassIsland.Core.Attributes;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.Components;

/// <summary>
/// GroupComponent.xaml 的交互逻辑
/// </summary>
[ContainerComponent]
[ComponentInfo("C911D762-107F-40C6-84CC-0146AB3C86B1", "分组组件", PackIconKind.SelectAll, "将多个组件组合到一个组件中显示。")]
public partial class GroupComponent
{
    public GroupComponent()
    {
        InitializeComponent();
    }
}