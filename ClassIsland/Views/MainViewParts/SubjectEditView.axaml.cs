using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Extensions.UI;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using ClassIsland.Extensions;
using ClassIsland.Shared;
using ClassIsland.ViewModels.MainViewParts;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views.MainViewParts;

public partial class SubjectEditView : ViewBase
{
    private readonly IProfileService _profileService = IAppHost.GetService<IProfileService>();
    private bool _isLeaving;

    public SubjectEditView(Guid subjectId, bool isNew = false,
        string initialName = "")
    {
        ViewModel = new SubjectEditViewModel(_profileService, subjectId, isNew, initialName);
        InitializeComponent();
        DataContext = ViewModel;
        Closing += SubjectEditView_OnClosing;
    }

    public SubjectEditViewModel ViewModel { get; }

    public event Action? Saved;

    private void SubjectEditView_OnClosing(object? sender, ViewClosingEventArgs e)
    {
        if (_isLeaving)
        {
            return;
        }

        try
        {
            ViewModel.Commit();
            Saved?.Invoke();
        }
        catch (Exception exception)
        {
            if (e.IsCancelable)
            {
                e.Cancel = true;
            }
            ToastsHelper.ShowErrorToast(this, "无法保存科目", exception);
        }
    }

    private void CommitField_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.Commit();
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "无法保存科目", exception);
        }
    }

    private async void MoreButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control anchor)
        {
            return;
        }

        var menu = new MenuFlyout();
        if (!ViewModel.IsNew)
        {
            var copy = new MenuItem
            {
                Header = "复制",
                Icon = new FluentIcon(ClassIsland.Core.Icons.FluentIcons.CopyRegular)
            };
            copy.Click += async (_, _) => await CopyAsync();
            var delete = new MenuItem
            {
                Header = "删除",
                Icon = new FluentIcon(ClassIsland.Core.Icons.FluentIcons.DeleteRegular)
            };
            delete.Click += async (_, _) => await DeleteAsync();
            menu.Items.Add(copy);
            menu.Items.Add(delete);
        }
        menu.ShowAt(anchor);
        await Task.CompletedTask;
    }

    private async Task CopyAsync()
    {
        var copy = Shared.Helpers.ConfigureFileHelper.CopyObject(ViewModel.Subject);
        var id = Guid.NewGuid();
        _profileService.Profile.Subjects.Add(id, copy);
        try
        {
            _profileService.SaveProfile(() => _profileService.Profile.Subjects.Remove(id));
            ToastsHelper.ShowToast(this, "已复制科目。");
            await AssociatedViewHost!.PushDrawerView(new SubjectEditView(id));
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "无法复制科目", exception);
        }
    }

    private async Task DeleteAsync()
    {
        var result = await new FAContentDialog
        {
            Title = "删除科目",
            Content = "使用此科目的课程将显示为“未指定科目”。",
            PrimaryButtonText = "删除",
            SecondaryButtonText = "取消",
            DefaultButton = FAContentDialogButton.Primary
        }.ShowAsyncAuto();
        if (result != FAContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            _isLeaving = true;
            ViewModel.Delete();
            await AssociatedViewHost!.HideDrawer();
        }
        catch (Exception exception)
        {
            _isLeaving = false;
            ToastsHelper.ShowErrorToast(this, "无法删除科目", exception);
        }
    }
}
