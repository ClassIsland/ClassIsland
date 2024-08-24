using System;
using System.Windows.Controls;

using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Models.AttachedSettings;

namespace ClassIsland.Controls;

/// <summary>
/// TestAttachedSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class TestAttachedSettingsControl : UserControl, IAttachedSettingsControlBase
{
    public TestAttachedSettingsControl()
    {
        InitializeComponent();
    }

    public AttachableSettingsObject? AttachedTarget { get; set; }

    public IAttachedSettingsHelper AttachedSettingsControlHelper { get; set; } =
        new AttachedSettingsControlHelper<TestAttachedSettings>(new Guid("6A692B17-4C37-4378-98EC-42730FB28C7C"),
            new TestAttachedSettings());


    public TestAttachedSettings? Settings =>
        (TestAttachedSettings?)((AttachedSettingsControlHelper<TestAttachedSettings>)AttachedSettingsControlHelper)
        .AttachedSettings;

}