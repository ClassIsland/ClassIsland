using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.Automation.Triggers;
using ReactiveUI;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.trayMenu", "从托盘菜单运行时", "\ue075")]
public class TrayMenuTrigger(TrayMenuTriggerHandlerService trayMenuTriggerHandlerService) : TriggerBase<TrayMenuTriggerSettings>
{
    public TrayMenuTriggerHandlerService TrayMenuTriggerHandlerService { get; } = trayMenuTriggerHandlerService;

    private NativeMenuItem MenuItem { get; } = new("");

    private IDisposable? _headerUpdateObserver;
    
    public override void Loaded()
    {
        MenuItem.Click += MenuItemOnClick;
        TrayMenuTriggerHandlerService.AddMenuItem(MenuItem);
        UpdateMenuItem();
        _headerUpdateObserver = Settings.ObservableForProperty(x => x.Header)
            .Skip(1).Subscribe(_ => UpdateMenuItem());
    }

    public override void UnLoaded()
    {
        MenuItem.Click -= MenuItemOnClick;
        TrayMenuTriggerHandlerService.RemoveMenuItem(MenuItem);
        _headerUpdateObserver?.Dispose();
    }

    private void UpdateMenuItem()
    {
        MenuItem.Header = Settings.Header;
    }
    
    private void MenuItemOnClick(object? sender, EventArgs e)
    {
        if (Settings.IsRevert)
        {
            TriggerRevert();
        }
        else
        {
            Trigger();
        }
    }
}