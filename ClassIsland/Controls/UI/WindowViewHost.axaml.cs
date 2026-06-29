using System;
using System.Collections.Generic;
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

    public void Show(IViewHost owner)
    {
        if (owner is WindowViewHost host)
        {
            base.Show(host);   
        }
        else
        {
            base.Show();
        }

        _isShowed = true;
    }

    public async Task ShowView(ViewBase view)
    {
        if (!ActivatedViews.Contains(view))
        {
            throw new InvalidOperationException("视图必须已经激活才能显示。");
        }

        if (!_isShowed)
        {
            Show();
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

        await NavigationPage.PopAsync();

        return true;
    }
}