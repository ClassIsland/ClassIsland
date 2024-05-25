using System.Windows.Controls;

using ClassIsland.Models;

namespace ClassIsland.Controls.MiniInfoProvider
{
    /// <summary>
    /// CountDownMiniInfoProviderControl.xaml 的交互逻辑
    /// </summary>
    public partial class CountDownMiniInfoProviderControl : UserControl
    {
        public CountDownMiniInfoProviderSettings Settings { get; }

        public CountDownMiniInfoProviderControl(CountDownMiniInfoProviderSettings settings)
        {
            Settings = settings;
            InitializeComponent();
        }
    }
}
