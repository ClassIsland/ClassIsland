using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Extensions.UI;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using ClassIsland.ViewModels.MainViewParts;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views.MainViewParts;

public partial class ProfileManagementView : ViewBase
{
    private readonly IProfileService _profileService = IAppHost.GetService<IProfileService>();
    private readonly SettingsService _settingsService;
    private readonly ProfileManagementViewModel _viewModel;
    private bool _isSwitching;

    public ProfileManagementView()
    {
        _settingsService = IAppHost.GetService<SettingsService>();
        _viewModel = new ProfileManagementViewModel(_settingsService);
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.RefreshProfiles();
    }

    private void RefreshProfiles() => _viewModel.RefreshProfiles();

    private async void ProfileRadioButton_OnChecked(object? sender, RoutedEventArgs e)
    {
        if (_isSwitching || sender is not RadioButton { IsChecked: true, DataContext: ProfileEntry entry } ||
            entry.IsSelected && string.Equals(entry.Name, _settingsService.Settings.SelectedProfile,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _isSwitching = true;
        try
        {
            var result = await new FAContentDialog
            {
                Title = "切换档案",
                Content = $"切换到“{entry.Name}”需要重启应用。是否继续？",
                PrimaryButtonText = "切换并重启",
                SecondaryButtonText = "取消",
                DefaultButton = FAContentDialogButton.Primary
            }.ShowAsyncAuto();
            if (result != FAContentDialogResult.Primary)
            {
                RefreshProfiles();
                return;
            }

            _profileService.SaveProfile();
            _settingsService.Settings.SelectedProfile = entry.Name;
            AppBase.Current.Restart();
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "无法切换档案", exception);
            RefreshProfiles();
        }
        finally
        {
            _isSwitching = false;
        }
    }

    private async void MoreButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control anchor)
        {
            return;
        }

        var menu = new MenuFlyout();
        var create = new MenuItem { Header = "新建档案", Icon = new FluentIcon(Core.Icons.FluentIcons.AddRegular) };
        create.Click += async (_, _) => await CreateProfileAsync();
        var refresh = new MenuItem { Header = "刷新", Icon = new FluentIcon(Core.Icons.FluentIcons.ArrowClockwiseRegular) };
        refresh.Click += (_, _) => RefreshProfiles();
        var folder = new MenuItem { Header = "打开文件夹", Icon = new FluentIcon(Core.Icons.FluentIcons.FolderOpenRegular) };
        folder.Click += (_, _) => PlatformServices.LauncherService.LaunchPath(ProfileService.ProfilePath);
        menu.Items.Add(create);
        menu.Items.Add(refresh);
        menu.Items.Add(folder);
        menu.ShowAt(anchor);
        await Task.CompletedTask;
    }

    private async Task CreateProfileAsync()
    {
        var box = new TextBox();
        var result = await new FAContentDialog
        {
            Title = "新建档案",
            Content = new Field { Label = "档案名称", Content = box, Suffix = ".json" },
            PrimaryButtonText = "新建",
            SecondaryButtonText = "取消",
            DefaultButton = FAContentDialogButton.Primary
        }.ShowAsyncAuto();
        if (result != FAContentDialogResult.Primary || string.IsNullOrWhiteSpace(box.Text))
        {
            return;
        }

        var path = Path.Combine(ProfileService.ProfilePath, $"{box.Text.Trim()}.json");
        if (File.Exists(path))
        {
            ToastsHelper.ShowWarningToast(this, "同名档案已经存在。");
            return;
        }

        try
        {
            var profile = ClassIsland.Services.ProfileService.CreateProfile(true);
            await File.WriteAllTextAsync(path,
                System.Text.Json.JsonSerializer.Serialize(profile));
            RefreshProfiles();
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "无法创建档案", exception);
        }
    }

    private async void CopyButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string name })
        {
            return;
        }
        var source = Path.Combine(ProfileService.ProfilePath, name);
        var target = Path.Combine(ProfileService.ProfilePath,
            Path.GetFileNameWithoutExtension(name) + " - 副本.json");
        try
        {
            File.Copy(source, target, false);
            RefreshProfiles();
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "无法复制档案", exception);
        }
        await Task.CompletedTask;
    }

    private async void RenameButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string name })
        {
            return;
        }
        var box = new TextBox { Text = Path.GetFileNameWithoutExtension(name) };
        var result = await new FAContentDialog
        {
            Title = "重命名档案",
            Content = new Field { Label = "档案名称", Content = box, Suffix = ".json" },
            PrimaryButtonText = "重命名",
            SecondaryButtonText = "取消",
            DefaultButton = FAContentDialogButton.Primary
        }.ShowAsyncAuto();
        if (result != FAContentDialogResult.Primary || string.IsNullOrWhiteSpace(box.Text))
        {
            return;
        }

        var source = Path.Combine(ProfileService.ProfilePath, name);
        var target = Path.Combine(ProfileService.ProfilePath, $"{box.Text.Trim()}.json");
        if (File.Exists(target))
        {
            ToastsHelper.ShowWarningToast(this, "同名档案已经存在。");
            return;
        }
        try
        {
            File.Move(source, target);
            if (string.Equals(Path.GetFileName(_profileService.CurrentProfilePath), name,
                    StringComparison.OrdinalIgnoreCase))
            {
                _profileService.CurrentProfilePath = Path.GetFileName(target);
            }
            if (string.Equals(_settingsService.Settings.SelectedProfile, name,
                    StringComparison.OrdinalIgnoreCase))
            {
                _settingsService.Settings.SelectedProfile = Path.GetFileName(target);
            }
            RefreshProfiles();
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "无法重命名档案", exception);
        }
    }

    private async void DeleteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string name })
        {
            return;
        }
        if (string.Equals(name, Path.GetFileName(_profileService.CurrentProfilePath),
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, _settingsService.Settings.SelectedProfile, StringComparison.OrdinalIgnoreCase))
        {
            ToastsHelper.ShowWarningToast(this, "无法删除当前正在使用的档案。");
            return;
        }

        var result = await new FAContentDialog
        {
            Title = "删除档案",
            Content = $"确定要删除“{name}”吗？此操作无法撤销。",
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
            File.Delete(Path.Combine(ProfileService.ProfilePath, name));
            RefreshProfiles();
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "无法删除档案", exception);
        }
    }
}
