using System;
using Avalonia.Controls;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Assists;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums;

namespace ClassIsland.Services.Automation.Triggers;

public class TrayMenuTriggerHandlerService
{
    private bool _isMenuItemInitialized = false;

    private NativeMenuItem EmptyItem { get; } = new("（空）")
    {
        IsEnabled = false
    };

    private NativeMenuItem MenuItem { get; } = new("运行工作流")
    {
        [NativeMenuItemAssist.IconSourceProperty] = new FluentIconSource("\uedbf")
    };
    private NativeMenu SubMenu { get; } = [];
    
    public TrayMenuTriggerHandlerService(ITaskBarIconService taskBarIconService)
    {
        TaskBarIconService = taskBarIconService;
        InitializeMenuItem();
    }

    private void InitializeMenuItem()
    {
        if (_isMenuItemInitialized)
        {
            return;
        }

        _isMenuItemInitialized = true;
        MenuItem.Menu = SubMenu;
        TaskBarIconService.MoreOptionsMenuItems.Add(MenuItem);
    }

    public void AddMenuItem(NativeMenuItem item)
    {
        if (SubMenu.Items.Count == 1 && SubMenu.Items[0] == EmptyItem)
        {
            SubMenu.Items.Clear();
        }
        
        SubMenu.Items.Add(item);
    }
    
    public void RemoveMenuItem(NativeMenuItem item)
    {
        SubMenu.Items.Remove(item);
        
        if (SubMenu.Items.Count == 0)
        {
            SubMenu.Items.Add(EmptyItem);
        }
    }

    public ITaskBarIconService TaskBarIconService { get; }
}