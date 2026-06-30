using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Controls;

namespace ClassIsland.Controls.UI;

public partial class WindowViewHost : MyWindow, IViewHost
{
    private HashSet<ViewBase> ActivatedViews { get; } = [];

    private bool _isShowed = false;

    public WindowViewHost()
    {
        DataContext = this;
        InitializeComponent();
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        var view = ActivatedViews.LastOrDefault();
        if (view == null)
        {
            return;
        }

        if (view.ViewDeactivating() || e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown)
        {
            foreach (var view1 in ActivatedViews)
            {
                if (view1 != view)
                {
                    view.ViewDeactivating();
                }
                view1.ViewDeactivated();
            }
            return;
        }

        e.Cancel = true;
    }


    private WindowViewHost? MyOwner { get; set; }

    IViewHost? IViewHost.Owner => MyOwner;

    public bool ActivateView(ViewBase view)
    {
        if (ActivatedViews.Contains(view))
        {
            return false;
        }

        if (!view.ViewActivating(this))
        {
            return false;
        }
        ActivatedViews.Add(view);
        view.ViewActivated(this);

        return true;
    }

    public bool DeactivateView(ViewBase view)
    {
        if (!ActivatedViews.Contains(view))
        {
            return false;
        }

        if (!view.ViewDeactivating())
        {
            return false;
        }
        ActivatedViews.Remove(view);
        view.ViewDeactivated();
        
        return true;
    }

    public override void Show()
    {
        base.Show();
        _isShowed = true;
    }

    public void Show(IViewHost owner)
    {
        if (owner is WindowViewHost host)
        {
            ShowDialog(host);   
            _isShowed = true;
        }
        else
        {
            Show();
        }
        
    }

    public async Task ShowView(ViewBase view)
    {
        if (!ActivatedViews.Contains(view))
        {
            throw new InvalidOperationException("视图必须已经激活到此视图宿主才能显示。");
        }

        if (!_isShowed)
        {
            Show();
        }

        await NavigationPage.PushAsync(view);
    }

    public async Task ShowViewModal(ViewBase view, ViewBase owner)
    {
        if (!ActivatedViews.Contains(view))
        {
            throw new InvalidOperationException("视图必须已经激活到此视图宿主才能显示。");
        }

        if (owner.AssociatedViewHost == null)
        {
            throw new InvalidOperationException("视图所有者必须已经激活到此视图宿主才能显示。");
        }
        
        if (!_isShowed)
        {
            Show(owner.AssociatedViewHost);
        }

        await NavigationPage.PushAsync(view);
    }

    public async Task<bool> HideView(ViewBase view)
    {
        if (!ActivatedViews.Contains(view))
        {
            throw new InvalidOperationException("视图必须已经激活才能隐藏。");
        }

        if (!Equals(NavigationPage.Content, view))
        {
            return false;
        }

        if (!DeactivateView(view))
        {
            return false;
        }

        await NavigationPage.PopAsync();

        return true;
    }

    private void NavigationPage_OnPopped(object? sender, NavigationEventArgs e)
    {
        if (e.Page is not ViewBase viewBase)
        {
            return;
        }
        viewBase.ViewDeactivating();
        viewBase.ViewDeactivated();
    }
}