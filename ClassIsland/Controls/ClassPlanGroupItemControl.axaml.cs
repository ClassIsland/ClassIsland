using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;
using ClassIsland.Views;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Controls;

/// <summary>
/// ClassPlanGroupItemControl.xaml 的交互逻辑
/// </summary>
public sealed partial class ClassPlanGroupItemControl : UserControl, INotifyPropertyChanged
{
    public static readonly StyledProperty<ClassPlanGroup> ItemProperty = AvaloniaProperty.Register<ClassPlanGroupItemControl, ClassPlanGroup>(
        nameof(Item));

    public ClassPlanGroup Item
    {
        get => GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public static readonly StyledProperty<Guid> KeyProperty = AvaloniaProperty.Register<ClassPlanGroupItemControl, Guid>(
        nameof(Key));

    public Guid Key
    {
        get => GetValue(KeyProperty);
        set => SetValue(KeyProperty, value);
    }
    
    private bool _isRenaming = false;

    private IProfileService ProfileService { get; } = App.GetService<IProfileService>();

    private readonly Guid _parentDialogId;
    private bool _isProtected = false;

    public bool IsProtected
    {
        get => _isProtected;
        set
        {
            if (value == _isProtected) return;
            _isProtected = value;
            OnPropertyChanged();
        }
    }

    public bool IsRenaming
    {
        get => _isRenaming;
        set
        {
            if (value == _isRenaming) return;
            _isRenaming = value;
            OnPropertyChanged();
        }
    }

    public ClassPlanGroupItemControl()
    {
        InitializeComponent();
        this.GetObservable(KeyProperty).Subscribe(_ =>
        {
            var policy = IAppHost.GetService<IManagementService>().Policy;
            IsProtected = Key == ClassPlanGroup.DefaultGroupGuid ||
                          Key == ClassPlanGroup.GlobalGroupGuid ||
                          policy.DisableProfileEditing ||
                          policy.DisableProfileClassPlanEditing;
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void MenuItemRename_OnClick(object sender, RoutedEventArgs e)
    {
        IsRenaming = true;
        MainTextBox.Focus();
        MainTextBox.SelectAll();
    }

    private void TextBox_OnLostFocus(object sender, RoutedEventArgs e)
    {
        IsRenaming = false;
    }

    private void TextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Avalonia.Input.Key.Enter or Avalonia.Input.Key.Escape)
        {
            IsRenaming = false;
            Focus();
        }
    }

    private void MenuItemTemp_OnClick(object sender, RoutedEventArgs e)
    {
        ProfileService.SetupTempClassPlanGroup(Key);
    }

    private async void MenuItemDisband_OnClick(object sender, RoutedEventArgs e)
    {
        var result = await new ContentDialog()
        {
            Title = "解散课表群",
            Content = this.FindResource("DisbandConfirmDialog"),
            PrimaryButtonText = "解散",
            SecondaryButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        }.ShowAsync(TopLevel.GetTopLevel(this));
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        ProfileService.Profile.DisbandClassPlanGroup(Key);
    }

    private async void MenuItemDelete_OnClick(object sender, RoutedEventArgs e)
    {
        var result = await new ContentDialog()
        {
            Title = "解散课表群",
            Content = this.FindResource("DeleteConfirmDialog"),
            PrimaryButtonText = "删除",
            SecondaryButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        }.ShowAsync(TopLevel.GetTopLevel(this));
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        ProfileService.Profile.DeleteClassPlanGroup(Key);
    }

    private void ToggleButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton button)
        {
            return;
        }

        if (button.IsChecked == true)
        {
            IAppHost.GetService<IProfileService>().Profile.SelectedClassPlanGroupId = Key;
        }
    }
}
