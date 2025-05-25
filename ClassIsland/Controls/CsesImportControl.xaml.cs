using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Shared;
using ClassIsland.Shared.Extensions;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Profile;
using CsesSharp;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Controls;

/// <summary>
/// CsesImportControl.xaml 的交互逻辑
/// </summary>
public partial class CsesImportControl : UserControl
{
    public static readonly DependencyProperty ImportTypeProperty = DependencyProperty.Register(
        nameof(ImportType), typeof(int), typeof(CsesImportControl), new PropertyMetadata(default(int)));

    public int ImportType
    {
        get { return (int)GetValue(ImportTypeProperty); }
        set { SetValue(ImportTypeProperty, value); }
    }

    public static readonly DependencyProperty NewProfileNameProperty = DependencyProperty.Register(
        nameof(NewProfileName), typeof(string), typeof(CsesImportControl), new PropertyMetadata(""));

    public string NewProfileName
    {
        get { return (string)GetValue(NewProfileNameProperty); }
        set { SetValue(NewProfileNameProperty, value); }
    }

    public static readonly DependencyProperty SourceFilePathProperty = DependencyProperty.Register(
        nameof(SourceFilePath), typeof(string), typeof(CsesImportControl), new PropertyMetadata(""));

    public string SourceFilePath
    {
        get { return (string)GetValue(SourceFilePathProperty); }
        set { SetValue(SourceFilePathProperty, value); }
    }

    private IProfileService ProfileService { get; } = IAppHost.GetService<IProfileService>();

    public CsesImportControl()
    {
        InitializeComponent();
    }

    private async void ButtonOk_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var csesProfile = CsesLoader.LoadFromYamlFile(SourceFilePath);
            var templateProfileJson =
                await new StreamReader(
                        Application.GetResourceStream(new Uri("/Assets/default-subjects.json", UriKind.Relative))!
                            .Stream)
                    .ReadToEndAsync();
            var templateProfile = JsonSerializer.Deserialize<Profile>(templateProfileJson);
            var profile = csesProfile.ToClassIslandObject(ImportType == 0 ? ProfileService.Profile : templateProfile);
            if (ImportType == 1)
            {
                var path = System.IO.Path.Combine("./Profiles", NewProfileName + ".json");
                if (File.Exists(path))
                {
                    throw new InvalidOperationException($"无法导入课表：{path} 已存在。");
                }
                ConfigureFileHelper.SaveConfig(path, profile);
            }
            DialogHost.CloseDialogCommand.Execute(true, this);
        }
        catch (Exception exception)
        {
            var logger = IAppHost.GetService<ILogger<CsesImportControl>>();
            logger.LogError(exception, "导入 CSES 课表失败");
            CommonDialog.ShowError($"无法导入 CSES 课表：{exception.Message}");
        }
    }
}