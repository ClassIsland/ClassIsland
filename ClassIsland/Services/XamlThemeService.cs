using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.XamlTheme;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ClassIsland.Services;

public class XamlThemeService : IXamlThemeService
{
    public ILogger<XamlThemeService> Logger { get; }
    private ResourceDictionary RootResourceDictionary { get; } = new();

    private Window MainWindow { get; } = AppBase.Current.MainWindow!;

    public static readonly string ThemesPath = Path.Combine(App.AppConfigPath, "Themes");

    public ObservableCollection<ThemeInfo> Themes { get; } = [];

    public XamlThemeService(ILogger<XamlThemeService> logger)
    {
        Logger = logger;
        var resourceBoarder = VisualTreeUtils.FindChildVisualByName<Border>(MainWindow, "ResourceLoaderBorder");
        resourceBoarder?.Resources.MergedDictionaries.Add(RootResourceDictionary);
        LoadAllThemes();
    }

    public void LoadAllThemes()
    {
        RootResourceDictionary.MergedDictionaries.Clear();
        Themes.Clear();
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        foreach (var i in Directory.GetDirectories(ThemesPath))
        {
            var manifest = new ThemeManifest()
            {
                Name = Path.GetFileName(i),
                Id = Path.GetFileName(i),
            };
            var themeInfo = new ThemeInfo
            {
                Path = Path.GetFullPath(i)
            };
            try
            {
                if (File.Exists(Path.Combine(i, "manifest.yml")))
                {
                    var yaml = File.ReadAllText(Path.Combine(i, "manifest.yml"));
                    manifest = deserializer.Deserialize<ThemeManifest>(yaml);
                }

                themeInfo.Manifest = manifest;
                themeInfo.Path = Path.GetFullPath(i);
                if (themeInfo.IsEnabled)
                {
                    LoadTheme(Path.Combine(i, "Theme.xaml"));
                    themeInfo.IsLoaded = true;
                }
            }
            catch (Exception e)
            {
                themeInfo.IsError = true;
                themeInfo.Error = e;
                Logger.LogError(e, "无法加载主题 {}", i);
            }
            Themes.Add(themeInfo);
        }
    }

    public void LoadTheme(string themePath)
    {
        Logger.LogInformation("正在加载主题 {}", themePath);
        var themeResourceDictionary = new ResourceDictionary
        {
            Source = new Uri(Path.GetFullPath(themePath))
        };
        RootResourceDictionary.MergedDictionaries.Add(themeResourceDictionary);
    }
}