using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using ClassIsland.Core;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Grpc.Core.Logging;
using Microsoft.Extensions.Logging;
using Sentry;

namespace ClassIsland.Views;

public partial class ProfileSettingsWindow : MyWindow
{
    private bool _isOpen = false;

    public ProfileSettingsViewModel ViewModel { get; } = IAppHost.GetService<ProfileSettingsViewModel>();

    private ILogger<ProfileSettingsWindow> Logger => ViewModel.Logger;

    public ProfileSettingsWindow()
    {
        DataContext = this;
        InitializeComponent();
    }

    #region Misc

    private void OpenDrawer(string key)
    {
        ViewModel.IsDrawerOpen = true;
        if (this.FindResource(key) is { } o)
        {
            ViewModel.DrawerContent = o;
        }
    }

    public async void Open()
    {
        if (!_isOpen)
        {
            if (!await ViewModel.ManagementService.AuthorizeByLevel(ViewModel.ManagementService.CredentialConfig
                    .EditProfileAuthorizeLevel))
            {
                return;
            }

            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.open");
            _isOpen = true;
            Show();
        }
        else
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
        }
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = true;
        _isOpen = false;
        Hide();
    }

    #endregion

    #region Profile

    private void RefreshProfiles()
    {
        ViewModel.Profiles = new ObservableCollection<string>
        (from i in Directory.GetFiles(Services.ProfileService.ProfilePath)
            where i.EndsWith(".json")
            select Path.GetFileName(i));
    }

    private void ButtonSaveProfile_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.ProfileService.SaveProfile();
            this.ShowToast(new ToastMessage($"已保存到 {ViewModel.ProfileService.CurrentProfilePath}")
            {
                Severity = InfoBarSeverity.Success
            });
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "无法保存档案");
            this.ShowErrorToast("无法保存档案", exception);
        }
    }

    private async void ButtonCreateProfile_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.create");
        ViewModel.CreateProfileName = "";
        var textBox = new TextBox();
        var r = await new ContentDialog()
        {
            Title = "新建档案",
            Content = new Field()
            {
                Content = textBox,
                Label = "档案名称",
                Suffix = ".json"
            },
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "新建",
            SecondaryButtonText = "取消"
        }.ShowAsync();

        var path = Path.Combine(Services.ProfileService.ProfilePath, $"{textBox.Text}.json");
        if (r != ContentDialogResult.Primary || File.Exists(path))
        {
            return;
        }

        var profile = new Profile();
        var subject =
            await new StreamReader(AssetLoader.Open(new Uri("avares://ClassIsland/Assets/default-subjects.json",
                UriKind.Absolute))).ReadToEndAsync();
        profile.Subjects = JsonSerializer.Deserialize<Profile>(subject)!.Subjects;
        var json = JsonSerializer.Serialize(profile);
        await File.WriteAllTextAsync(path, json);
        RefreshProfiles();
    }

    private void ButtonOpenProfileFolder_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.openFolder");
        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath(Services.ProfileService.ProfilePath),
            UseShellExecute = true
        });
    }

    private void ButtonRefreshProfiles_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.refresh");
        RefreshProfiles();
    }

    private async void MenuItemRenameProfile_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.rename");
        ViewModel.RenameProfileName = Path.GetFileNameWithoutExtension(ViewModel.SelectedProfile);
        var textBox = new TextBox()
        {
            Text = ViewModel.RenameProfileName
        };
        var r = await new ContentDialog()
        {
            Title = "重命名档案",
            Content = new Field()
            {
                Content = textBox,
                Label = "档案名称",
                Suffix = ".json"
            },
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "重命名",
            SecondaryButtonText = "取消"
        }.ShowAsync();

        var raw = Path.Combine(Services.ProfileService.ProfilePath, $"{ViewModel.SelectedProfile}");
        var path = Path.Combine(Services.ProfileService.ProfilePath, $"{textBox.Text}.json");
        if (r != ContentDialogResult.Primary || !File.Exists(raw))
        {
            return;
        }

        if (File.Exists(path))
        {
            this.ShowToast(new ToastMessage()
            {
                Message = "无法重命名档案，因为已存在一个相同名称的档案",
                Severity = InfoBarSeverity.Warning
            });
            return;
        }

        File.Move(raw, path);
        if (ViewModel.ProfileService.CurrentProfilePath == Path.GetFileName(raw))
        {
            ViewModel.ProfileService.CurrentProfilePath = Path.GetFileName(path);
            ViewModel.SettingsService.Settings.SelectedProfile = Path.GetFileName(path);
        }

        RefreshProfiles();
    }

    private async void MenuItemDeleteProfile_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.DeleteConfirmField = "";
        var path = Path.Combine(Services.ProfileService.ProfilePath, $"{ViewModel.SelectedProfile}");
        if (ViewModel.SelectedProfile == ViewModel.ProfileService.CurrentProfilePath ||
            ViewModel.SelectedProfile == ViewModel.SettingsService.Settings.SelectedProfile)
        {
            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.remove",
                tags: new Dictionary<string, string>
                {
                    { "Reason", "正在删除已加载或将要加载的档案。" },
                    { "IsSuccess", "false" }
                });
            this.ShowToast(new ToastMessage("无法删除已加载或将要加载的档案")
            {
                Severity = InfoBarSeverity.Warning
            });
            return;
        }

        var textBox = new TextBox();
        var r = await new ContentDialog()
        {
            Title = "删除档案",
            Content = $"您确定要删除档案 {ViewModel.ProfileService.CurrentProfilePath} 吗？此操作无法撤销，档案内的课表、时间表、科目等信息都将被删除！",
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "删除",
            SecondaryButtonText = "取消"
        }.ShowAsync();

        if (r == ContentDialogResult.Primary)
        {
            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.remove",
                tags: new Dictionary<string, string>
                {
                    { "IsSuccess", "true" }
                });
            File.Delete(path);
        }
        else
        {
            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.remove",
                tags: new Dictionary<string, string>
                {
                    { "Reason", "用户取消操作。" },
                    { "IsSuccess", "false" }
                });
        }

        RefreshProfiles();
    }

    private void MenuItemProfileDuplicate_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.profile.duplicate");
        var raw = Path.Combine(Services.ProfileService.ProfilePath, $"{ViewModel.SelectedProfile}");
        var d = Path.GetFileNameWithoutExtension(ViewModel.SelectedProfile) + " - 副本.json";
        var d1 = Path.Combine(Services.ProfileService.ProfilePath, $"{d}");
        File.Copy(raw, d1);
        RefreshProfiles();
    }

    private void ButtonOpenProfileManager_OnClick(object? sender, RoutedEventArgs e)
    {
        RefreshProfiles();
        OpenDrawer("ProfileManager");
    }

    [RelayCommand]
    private void ProfileSelectionRadioButtonToggled(string name)
    {
        ViewModel.SettingsService.Settings.SelectedProfile = name;
        var action = new Button()
        {
            Content = "立即重启"
        };
        action.Click += (sender, args) => AppBase.Current.Restart();
        this.ShowToast(new ToastMessage()
        {
            Title = "需要重启",
            Message = "切换档案需要重启应用以生效",
            AutoClose = false,
            ActionContent = action
        });
    }

    #endregion

    #region ClassPlans
    
    private void ButtonCreateTempOverlayClassPlan_OnClick(object? sender, RoutedEventArgs e)
    {
        var key = ViewModel.ProfileService.Profile.ClassPlans
            .FirstOrDefault(x => x.Value == ViewModel.SelectedClassPlan).Key;
        var id = ViewModel.ProfileService.CreateTempClassPlan(key,
            ViewModel.TempOverlayClassPlanTimeLayoutId,
            ViewModel.OverlayEnableDateTime);
        if (id is { } guid)
        {
            ViewModel.SelectedClassPlan = ViewModel.ProfileService.Profile.ClassPlans[guid];
            OpenDrawer("ClassPlansInfoEditor");
            FlyoutHelper.CloseAncestorFlyout(sender);
        }
        else
        {
            this.ShowToast(new ToastMessage("在这一天已存在一个临时层课表，无法创建新的临时层课表。")
            {
                Severity = InfoBarSeverity.Warning,
                Duration = TimeSpan.FromSeconds(5)
            });
        }

    }

    private void ButtonOpenClassPlanDetails_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenDrawer("ClassPlansInfoEditor");
    }

    private void ButtonAddClassPlan_OnClick(object? sender, RoutedEventArgs e)
    {
        var newClassPlan = new ClassPlan();
        ViewModel.ProfileService.Profile.ClassPlans.Add(Guid.NewGuid(), newClassPlan);
        ViewModel.SelectedClassPlan = newClassPlan;
        OpenDrawer("ClassPlansInfoEditor");
    }

    private void ButtonDeleteSelectedClassPlan_OnClick(object? sender, RoutedEventArgs e)
    {
        var k = ViewModel.ProfileService.Profile.ClassPlans
            .FirstOrDefault(x => x.Value == ViewModel.SelectedClassPlan).Key;
        ViewModel.ProfileService.Profile.ClassPlans.Remove(k);
        foreach (var (key, _) in ViewModel.ProfileService.Profile.OrderedSchedules.Where(x => x.Value.ClassPlanId == k).ToList())
        {
            ViewModel.ProfileService.Profile.OrderedSchedules.Remove(key);
        }
        FlyoutHelper.CloseAncestorFlyout(sender);
    }

    private void ButtonOpenCreateOverlayClassPlanFlyout_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedClassPlan == null)
        {
            return;
        }
        ViewModel.OverlayEnableDateTime = ViewModel.ExactTimeService.GetCurrentLocalDateTime().Date;
        ViewModel.TempOverlayClassPlanTimeLayoutId = ViewModel.SelectedClassPlan.TimeLayoutId;
    }
    
    private void ButtonDuplicateClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        var s = ConfigureFileHelper.CopyObject(ViewModel.SelectedClassPlan);
        if (s == null)
        {
            return;
        }

        OpenDrawer("ClassPlansInfoEditor");
        ViewModel.ProfileService.Profile.ClassPlans.Add(Guid.NewGuid(), s);
        ViewModel.SelectedClassPlan = s;
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.classPlan.duplicate");
    }
    
    private void ButtonGoToTimeLayoutsPage_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.MasterPageTabSelectIndex = 1;
    }

    #endregion

    #region Subjects

    private void ButtonAddSubject_OnClick(object sender, RoutedEventArgs e)
    {
        //DataGridSubjects.CancelEdit();
        
        var isCreating = DataGridSubjects.SelectedIndex == ViewModel.ProfileService.Profile.Subjects.Count;
        
        DataGridSubjects.CancelEdit();
        DataGridSubjects.IsReadOnly = true;
        ViewModel.ProfileService.Profile.EditingSubjects.Add(new Subject());
        DataGridSubjects.IsReadOnly = false;
        DataGridSubjects.SelectedIndex = ViewModel.ProfileService.Profile.Subjects.Count - 1;
        //TextBoxSubjectName.Focus();
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.subject.create");
    }
    
    private void ButtonDuplicateSubject_OnClick(object sender, RoutedEventArgs e)
    {
        DataGridSubjects.CancelEdit();
        DataGridSubjects.IsReadOnly = true;
        foreach (var i in DataGridSubjects.SelectedItems)
        {
            var subject = i as Subject;
            var o = ConfigureFileHelper.CopyObject(subject);
            if (o == null)
            {
                continue;
            }

            ViewModel.ProfileService.Profile.EditingSubjects.Add(o);
        }
        DataGridSubjects.SelectedItem = ViewModel.ProfileService.Profile.EditingSubjects.Last();
        DataGridSubjects.IsReadOnly = false;
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.subject.duplicate");
    }

    private void ButtonDeleteSubject_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.subject.remove", tags: new Dictionary<string, string>
        {
            {"IsSuccess", "true"},
        });

        DataGridSubjects.CancelEdit();
        DataGridSubjects.IsReadOnly = true;
        var rm = new List<Subject>();
        foreach (var i in DataGridSubjects.SelectedItems)
        {
            if (i is Subject o)
            {
                rm.Add(o);
            }
        }
        var s = ViewModel.ProfileService.Profile.EditingSubjects;
        foreach (var t in rm)
        {
            s.Remove(t);
        }

        var revertButton = new Button()
        {
            Content = "撤销"
        };
        var toastMessage = new ToastMessage($"已删除 {rm.Count} 个科目。")
        {
            ActionContent = revertButton,
            Duration = TimeSpan.FromSeconds(10)
        };
        revertButton.Click += (o, args) =>
        {
            foreach (var subject in rm)
            {
                ViewModel.ProfileService.Profile.EditingSubjects.Add(subject);
            }
            toastMessage.Close();
        };
        this.ShowToast(toastMessage);
        DataGridSubjects.IsReadOnly = false;
    }
    #endregion


    
}