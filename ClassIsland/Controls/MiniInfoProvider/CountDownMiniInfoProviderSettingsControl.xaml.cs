using System.Windows.Controls;

using ClassIsland.Models;

namespace ClassIsland.Controls.MiniInfoProvider
{
    /// <summary>
    /// CountDownMiniInfoProviderSettingsControl.xaml 的交互逻辑
    /// </summary>
    public partial class CountDownMiniInfoProviderSettingsControl : UserControl
    {
        public CountDownMiniInfoProviderSettings Settings { get; }

        public CountDownMiniInfoProviderSettingsControl(CountDownMiniInfoProviderSettings settings)
        {
            Settings = settings;
            InitializeComponent();
        }
    }
}
