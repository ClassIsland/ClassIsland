using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Controls;

namespace ClassIsland.Controls.UI;

[PseudoClasses(":mobile")]
public partial class WindowViewHost : MyWindow, IViewHost
{
    public bool IsMobileMode { get; }
    
    private HashSet<ViewBase> ActivatedViews { get; } = [];

    private bool _isShowed = false;

    private bool _isClosed = false;

    public WindowViewHost(bool isMobileMode=false)
    {
        IsMobileMode = isMobileMode;
        DataContext = this;
        InitializeComponent();
        Closing += OnClosing;
        Closed += OnClosed;
        if (IsMobileMode)
        {
            Width = 360;
            Height = 800;
            PseudoClasses.Set(":mobile", true);
        }
        
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _isClosed = true;
        NavigationPage.PopAllModalsAsync(null);
        NavigationPage.PopToRootAsync(null);
        NavigationPage.ReplaceAsync(new ContentPage(), null);
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        var view = ActivatedViews.LastOrDefault();
        if (view == null)
        {
            return;
        }

        if (view.ViewDeactivating(e.CloseReason, e.IsProgrammatic, true) || e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown)
        {
            foreach (var view1 in ActivatedViews)
            {
                if (view1 != view)
                {
                    view.ViewDeactivating(e.CloseReason, e.IsProgrammatic, false);
                }
                view1.ViewDeactivated();
            }
            return;
        }

        e.Cancel = true;
    }


    private WindowViewHost? MyOwner { get; set; }

    IViewHost? IViewHost.Owner => MyOwner;

    public new void Activate()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        base.Activate();
    }

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

        if (!view.ViewDeactivating(WindowCloseReason.Undefined, true, true))
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
        Show(owner, false);
    }

    public void Show(IViewHost? owner, bool modal)
    {
        if (owner is WindowViewHost host)
        {
            if (modal)
            {
                ShowDialog(host);   
            }
            else
            {
                base.Show(host);
            }
            _isShowed = true;
        }
        else
        {
            Show();
        }
        
    }

    public async Task ShowView(ViewBase view, ViewBase? owner = null)
    {
        if (!ActivatedViews.Contains(view))
        {
            throw new InvalidOperationException("视图必须已经激活到此视图宿主才能显示。");
        }

        if (!_isShowed)
        {
            Show(owner?.AssociatedViewHost, true);
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
            Show(owner.AssociatedViewHost, true);
        }

        await NavigationPage.PushAsync(view);
    }

    public async Task<bool> HideView(ViewBase view)
    {
        if (!ActivatedViews.Contains(view))
        {
            throw new InvalidOperationException("视图必须已经激活才能隐藏。");
        }

        if (!Equals(NavigationPage.CurrentPage, view))
        {
            return false;
        }

        if (!DeactivateView(view))
        {
            return false;
        }

        if (NavigationPage.Pages?.Count() <= 1)
        {
            Close();
        }
        else
        {
            await NavigationPage.PopAsync();
        }

        return true;
    }

    private void NavigationPage_OnPopped(object? sender, NavigationEventArgs e)
    {
        if (_isClosed)
        {
            return;
        }
        if (e.Page is not ViewBase viewBase)
        {
            return;
        }
        viewBase.ViewDeactivating(WindowCloseReason.Undefined, true, true);
        viewBase.ViewDeactivated();
        ActivatedViews.Remove(viewBase);
    }
}