using System.Windows;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Shared;

namespace ClassIsland.Core.Controls.StickerControl;

/// <summary>
/// 表情包控件。可以在禁用彩蛋时自动切换到替代图标。
/// </summary>
public class StickerControl : IconControl.IconControl
{
    /// <summary>
    /// 彩蛋是否已经禁用
    /// </summary>
    public bool IsEasterEggDisabled
    {
        get
        {
            var managementService =
                IAppHost.Host?.Services.GetService(typeof(IManagementService)) as IManagementService;
            return managementService?.Policy?.DisableEasterEggs ?? false;
        }
    }
}