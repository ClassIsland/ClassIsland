using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using ClassIsland.Core;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Components;
using ClassIsland.Enums;
using ClassIsland.Models;
using ClassIsland.Models.External.ClassWidgets;
using ClassIsland.Models.NotificationProviderSettings;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Services;
using ClassIsland.Services.NotificationProviders;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Notification;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;
using FluentAvalonia.UI.Controls;
using IniParser;
using IniParser.Model;

namespace ClassIsland.Views;

public partial class DataTransferPage : UserControl
{
    public DataTransferViewModel ViewModel { get; } = IAppHost.GetService<DataTransferViewModel>();
    
    public DataTransferPage()
    {
        InitializeComponent();
    }
    
    private async void SettingsExpanderBrowse_OnClick(object? sender, RoutedEventArgs e)
    {
        
        if (ViewModel.BrowseAction == null || ViewModel.PerformImportAction == null)
        {
            return;
        }
        await ViewModel.BrowseAction();
        if (string.IsNullOrWhiteSpace(ViewModel.ImportSourcePath))
        {
            return;
        }

        await ViewModel.PerformImportAction();
    }
    
    private void ButtonTurnBack_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.PageIndex--;
    }

    #region ClassIsland
    
    private async void SettingsExpanderImportFromClassIsland1_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.BrowseAction = BrowseClassIslandData;
        ViewModel.PerformImportAction = BeginPerformClassIslandImport;
        ViewModel.PageIndex = 1;
        ViewModel.ImportSourcePath = "";
        ViewModel.ImportDescription = "支持从 1.x 版本的 ClassIsland 导入课表、组件配置、自动化配置、应用设置、部分插件和主题等数据。";
    }

    private async Task BrowseClassIslandData()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }

        var file = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "选择先前版本的 ClassIsland 实例",
            FileTypeFilter =
            [
                new FilePickerFileType("ClassIsland 可执行文件")
                {
                    Patterns = ["ClassIsland.exe"]
                }
            ]
        });
        if (file.Count <= 0)
        {
            return;
        }

        ViewModel.ImportSourcePath = Path.GetDirectoryName(file[0].Path.AbsolutePath) ?? "";
    }

    private async Task BeginPerformClassIslandImport()
    {
        var r = await new ContentDialog()
        {
            Title = "重启以继续",
            Content = "应用需要重启以继续导入操作，要重启以继续导入吗？",
            PrimaryButtonText = "重启并继续",
            SecondaryButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        }.ShowAsync(TopLevel.GetTopLevel(this));
        if (r != ContentDialogResult.Primary)
        {
            return;
        }

        var entry = ImportEntries.None;
        if (ViewModel.IsProfileSelected)
        {
            entry |= ImportEntries.Profiles;
        }
        if (ViewModel.IsSettingsSelected)
        {
            entry |= ImportEntries.Settings;
        }
        if (ViewModel.IsOtherConfigSelected)
        {
            entry |= ImportEntries.OtherConfig;
        }
        AppBase.Current.Restart(["--importV1", ViewModel.ImportSourcePath, "-m", "--importEntries", entry.ToString()]);
    }

    private void ImportSettings(string root)
    {
        var settings = ConfigureFileHelper.LoadConfigUnWrapped<Settings>(Path.Combine(root, "Settings.json"), false);
        if (settings.LastAppVersion < Version.Parse("1.7.0.0"))
        {
            throw new Exception("源 ClassIsland 版本必须在 1.7.0.x，才能进行导入。");
        }
        settings.MainWindowFont = MainWindow.DefaultFontFamilyKey;
        settings.AutoInstallUpdateNextStartup = false;
        settings.ShowEchoCaveWhenSettingsPageLoading = false;
        if (settings.WeatherIconId == "classisland.weatherIcons.materialDesign")
        {
            settings.WeatherIconId = "classisland.weatherIcons.fluentDesign";
        }
        ConfigureFileHelper.SaveConfig(Path.Combine(CommonDirectories.AppRootFolderPath, "Settings.json"), settings);
    }
    
    private void ImportConfig(string root)
    {
        Directory.Delete(Path.Combine(CommonDirectories.AppRootFolderPath, "Plugins"), true);
        
        FileFolderService.CopyFolder(Path.Combine(root, "Config"),
            Path.Combine(CommonDirectories.AppRootFolderPath, "Config"), true);
        FileFolderService.CopyFolder(Path.Combine(root, "Plugins"),
            Path.Combine(CommonDirectories.AppRootFolderPath, "Plugins"), true);
        if (!Directory.Exists(ComponentsService.ComponentSettingsPath))
        {
            Directory.CreateDirectory(ComponentsService.ComponentSettingsPath);
        }
        foreach (var path in Directory.GetFiles(Path.Combine(CommonDirectories.AppConfigPath, "Islands")))
        {
            var oldConfig =
                ConfigureFileHelper.LoadConfigUnWrapped<ObservableCollection<ComponentSettings>>(path, false);
            var newConfig = new ComponentProfile();
            var e = oldConfig.OrderBy(x => x.RelativeLineNumber)
                .GroupBy(x => x.RelativeLineNumber)
                .Select(x => new MainWindowLineSettings()
                {
                    IsMainLine = x.Key == 0,
                    Children = new ObservableCollection<ComponentSettings>(x)
                });
            newConfig.Lines = new ObservableCollection<MainWindowLineSettings>(e);
            ConfigureFileHelper.SaveConfig(Path.Combine(ComponentsService.ComponentSettingsPath, Path.GetFileName(path)), newConfig);
        }
    }
    
    [Obsolete]
    private void ImportProfile(string root)
    {
        FileFolderService.CopyFolder(Path.Combine(root, "Profiles"),
            Path.Combine(CommonDirectories.AppRootFolderPath, "Profiles"), true);
        foreach (var path in Directory.GetFiles(ProfileService.ProfilePath))
        {
            var config = ConfigureFileHelper.LoadConfigUnWrapped<Profile>(path, false);
            foreach (var tl in config.TimeLayouts)
            {
                foreach (var layoutItem in tl.Value.Layouts.Where(x =>
                             !string.IsNullOrWhiteSpace(x.StartSecond) && !string.IsNullOrWhiteSpace(x.EndSecond)))
                {
                    layoutItem.StartTime = DateTime.TryParse(layoutItem.StartSecond, out var r1)
                        ? r1.TimeOfDay
                        : TimeSpan.Zero;
                    layoutItem.EndTime = DateTime.TryParse(layoutItem.EndSecond, out var r2)
                        ? r2.TimeOfDay
                        : TimeSpan.Zero;
                }
            }
            ConfigureFileHelper.SaveConfig(Path.Combine(ProfileService.ProfilePath, Path.GetFileName(path)), config);
        }
    }

    [Obsolete]
    public async Task PerformClassIslandImport(string root, ImportEntries importEntries)
    {
        try
        {
            ViewModel.PageIndex = 3;
            await Task.Run(() =>
            {
                if ((importEntries & ImportEntries.Settings) == ImportEntries.Settings)
                {
                    ImportSettings(root);
                }
                if ((importEntries & ImportEntries.OtherConfig) == ImportEntries.OtherConfig)
                {
                    ImportConfig(root);
                }

                if ((importEntries & ImportEntries.Profiles) == ImportEntries.Profiles)
                {
                    ImportProfile(root);
                }
            });
            
            ViewModel.PageIndex = 4;
        }
        catch (Exception e)
        {
            this.ShowErrorToast("导入时发生意外错误", e);
        }

    }

    #endregion

    #region Class Widgets

    private double TryGetDoubleFromSection(PropertyCollection? dictionary, string key, double fallback) 
    {
        if (dictionary == null)
        {
            return fallback;
        }
        
        return dictionary[key] != null ? (double.TryParse(dictionary[key], out var r2) ? r2 : fallback) : fallback;
    }
    
    private bool TryGetBooleanFromSection(PropertyCollection? dictionary, string key, bool fallback) 
    {
        if (dictionary == null)
        {
            return fallback;
        }

        return dictionary[key] != null ? (int.TryParse(dictionary[key], out var r2) ? r2 == 1 : fallback) : fallback;
    }
    
    private string TryGetStringFromSection(PropertyCollection? dictionary, string key, string fallback) 
    {
        if (dictionary == null)
        {
            return fallback;
        }

        return dictionary[key] != null ? dictionary[key] : fallback;
    }
    
    private int TryGetIntFromSection(PropertyCollection? dictionary, string key, int fallback) 
    {
        if (dictionary == null)
        {
            return fallback;
        }

        return dictionary[key] != null ? (int.TryParse(dictionary[key], out var r2) ? r2 : fallback) : fallback;
    }

    private void ImportFromClassWidgets1_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.BrowseAction = BrowseClassWidgetsData;
        ViewModel.PerformImportAction = PerformClassWidgetsImportAction;
        ViewModel.PageIndex = 1;
        ViewModel.ImportSourcePath = "";
        ViewModel.ImportDescription = "支持从 Class Widgets 1 导入全部课表信息和大部分配置。";
    }
    
    private async Task BrowseClassWidgetsData()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }

        var file = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = "浏览 Class Widgets 数据目录"
        });
        if (file.Count <= 0)
        {
            return;
        }

        ViewModel.ImportSourcePath = file[0].Path.AbsolutePath;
    }

    private void ImportClassWidgetsProfile(string root, IniData ini)
    {
        var general = ini["General"];
        var profileName = TryGetStringFromSection(general, "schedule", "新课表 - 1.json");

        var profileCw = ConfigureFileHelper.LoadConfigUnWrapped<CwProfile>(Path.Combine(root, "config", "schedule", profileName));
        var profile = new Profile();
        var subjectJson = new StreamReader(AssetLoader.Open(new Uri("avares://ClassIsland/Assets/default-subjects.json"))).ReadToEnd();
        profile.Subjects = JsonSerializer.Deserialize<Profile>(subjectJson)!.Subjects;
        
        // Subjects
        var subjectsCache = profile.Subjects.ToDictionary(x => x.Value.Name, x => x.Key);
        foreach (var subjectName in profileCw.Schedule.Values.ToList().SelectMany(x => x))
        {
            subjectsCache.TryAdd(subjectName, Guid.NewGuid());
        }
        foreach (var subjectName in profileCw.ScheduleEven.Values.ToList().SelectMany(x => x))
        {
            subjectsCache.TryAdd(subjectName, Guid.NewGuid());
        }
        foreach (var (name, guid) in subjectsCache)
        {
            profile.Subjects.TryAdd(guid, new Subject()
            {
                Name = name
            });
        }
        
        // TimeLayouts
        // 注意！cw 课表结构中，计数从周一（0）开始。
        var parts = new Dictionary<string, CwProfileTimeSpan?>();
        var timeLayouts = new Dictionary<string, TimeLayout>();
        var timeLayoutsMap = new Dictionary<string, Guid>([
            new KeyValuePair<string, Guid>("default", Guid.NewGuid())
        ]);
        for (int i = 0; i < 7; i++)
        {
            timeLayoutsMap.Add(i.ToString(), Guid.NewGuid());
        }
        foreach (var (index, value) in profileCw.Part.OrderBy(x => int.Parse(x.Key)))
        {
            if (value.Count < 3 || value[0] is not JsonElement { ValueKind: JsonValueKind.Number } startHour 
                                || value[1] is not JsonElement { ValueKind: JsonValueKind.Number } endHour 
                                || value[2] is not JsonElement { ValueKind: JsonValueKind.String } type 
                                || profileCw.PartName.GetValueOrDefault(index) is not {} name)
            {
                parts.Add(index, null);
                continue;
            }
            parts.Add(index, new CwProfileTimeSpan()
            {
                Name = name,
                StartTime = new TimeSpan(startHour.GetInt32(), endHour.GetInt32(), 0),
                Type = type.GetRawText() switch
                {
                    "break" => CwProfileTimeSpan.TimeSpanType.Break,
                    _ => CwProfileTimeSpan.TimeSpanType.Part
                }
            });
        }

        foreach (var (id, timeLine) in profileCw.Timeline.Where(x => x.Value.Count > 0))
        {
            var timeLineNormalized = timeLine
                .Where(x => x.Key.Length == 3
                            && parts.GetValueOrDefault(x.Key[1].ToString()) != null
                            && int.TryParse(x.Key[1].ToString(), out _)
                            && double.TryParse(x.Value, out _))
                .OrderBy(x => int.Parse(x.Key[1].ToString()))
                .ToList();
            if (timeLineNormalized.Count <= 0)
            {
                continue;
            }

            var timeLayout = new TimeLayout();
            var groupIndex = 0;
            var prevGroup = timeLineNormalized[0].Key[1].ToString();
            var prevEnd = parts[prevGroup]!.StartTime;
            foreach (var (k, v) in timeLineNormalized)
            {
                var group = k[1].ToString();
                if (group != prevGroup)
                {
                    prevGroup = group;
                    groupIndex++;
                    timeLayout.Layouts.Add(new TimeLayoutItem()
                    {
                        TimeType = 2,
                        StartTime = parts[group]!.StartTime - TimeSpan.FromSeconds(1)
                    });
                    prevEnd = parts[group]!.StartTime;
                }

                var start = prevEnd;
                var end = prevEnd = start + TimeSpan.FromMinutes(double.Parse(v));
                timeLayout.Layouts.Add(new TimeLayoutItem()
                {
                    TimeType = k[0] == 'f' ? 1 : 0,
                    StartTime = start,
                    EndTime = end
                });
            }
            timeLayouts[id] = timeLayout;
        }

        foreach (var (id, timeLayout) in timeLayouts)
        {
            profile.TimeLayouts[timeLayoutsMap[id]] = timeLayout;
        }
        
        // ClassPlans
        // 注意！cw 课表结构中，计数从周一（0）开始。
        List<string> days = ["6", "0", "1", "2", "3", "4", "5"];
        List<string> daysName = ["周日", "周一", "周二", "周三", "周四", "周五", "周六"];
        var undefinedClassName = "未添加";
        for (int i = 0; i < 7; i++)
        {
            var day = days[i];
            var cpOdd = new ClassPlan()
            {
                TimeLayoutId = timeLayouts.ContainsKey(day) ? timeLayoutsMap[day] : timeLayoutsMap["default"],
                TimeLayouts = profile.TimeLayouts
            };
            var cpEven = new ClassPlan()
            {
                TimeLayoutId = timeLayouts.ContainsKey(day) ? timeLayoutsMap[day] : timeLayoutsMap["default"],
                TimeLayouts = profile.TimeLayouts
            };
            cpEven.TimeRule.WeekDay = cpOdd.TimeRule.WeekDay = i;
            cpOdd.Name = cpEven.Name = daysName[i];
            var shouldSplitClassPlans = profileCw.Schedule[day].Count != profileCw.ScheduleEven[day].Count;
            // 清除自动生成的课程安排
            cpEven.Classes.Clear();
            cpOdd.Classes.Clear();
            for (int j = 0; j < Math.Min(profileCw.Schedule[day].Count, profileCw.ScheduleEven[day].Count); j++)
            {
                var subjectOdd = profileCw.Schedule[day][j];
                var subjectEven = profileCw.ScheduleEven[day][j];
                cpOdd.Classes.Add(new ClassInfo()
                {
                    SubjectId = subjectsCache[subjectOdd]
                });
                cpEven.Classes.Add(new ClassInfo()
                {
                    SubjectId = subjectsCache[subjectEven]
                });
                if (subjectEven != subjectOdd)
                {
                    shouldSplitClassPlans = true;
                }
            }

            var oddValid = cpOdd.Classes.Any(x => x.SubjectId != subjectsCache[undefinedClassName]);
            var evenValid = cpEven.Classes.Any(x => x.SubjectId != subjectsCache[undefinedClassName]);
            var evenAdded = false;
            if (oddValid)
            {
                profile.ClassPlans.TryAdd(Guid.NewGuid(), cpOdd);
            }
            else if (evenValid)
            {
                profile.ClassPlans.TryAdd(Guid.NewGuid(), cpEven);
                evenAdded = true;
            }
            
            if (!shouldSplitClassPlans || !evenValid || !oddValid)
            {
                continue;
            }
            cpOdd.Name += "（单周）";
            cpEven.Name += "（双周）";
            cpOdd.TimeRule.WeekCountDivTotal = cpOdd.TimeRule.WeekCountDivTotal = 2;
            cpEven.TimeRule.WeekCountDiv = 0;
            cpOdd.TimeRule.WeekCountDiv = 1;
            if (!evenAdded)
            {
                profile.ClassPlans.TryAdd(Guid.NewGuid(), cpEven);
            }
        }
        
        ConfigureFileHelper.SaveConfig(Path.Combine(ProfileService.ProfilePath, $"cw_{profileName}"), profile);
    }
    
    private void ImportClassWidgetsSettings(string root, IniData ini)
    {
        var settingsService = IAppHost.GetService<SettingsService>();
        var settings = settingsService.Settings;
        
        // General
        var general = ini["General"];
        settings.Scale = TryGetDoubleFromSection(general, "scale", 1.0);
        settings.Opacity = TryGetDoubleFromSection(general, "opacity", 100) / 100;
        settings.HideOnClass = TryGetIntFromSection(general, "hide", 0) == 1;
        PlatformServices.DesktopService.IsAutoStartEnabled = TryGetBooleanFromSection(general, "auto_startup", false);
        settings.WindowLayer = TryGetIntFromSection(general, "pin_on_top", 0) switch
        {
            0 or 2 => 0,
            1 => 1,
            _ => 0
        };
        settings.Theme = TryGetIntFromSection(general, "color_mode", 2) switch
        {
            2 => 0,
            0 => 1,
            1 => 2,
            _ => 0
        };
        
        // Weather
        var weather = ini["Weather"];
        if (TryGetStringFromSection(weather, "api", "xiaomi_weather") == "xiaomi_weather")
        {
            settings.CityId = "weathercn:" + TryGetStringFromSection(weather, "city", "101010100");
            settings.WeatherLocationSource = 0;
        }
        
        // Toast
        var toast = ini["Toast"];
        settings.IsNotificationEnabled = true;
        settings.IsNotificationEffectEnabled =
            settings.AllowNotificationEffect = TryGetBooleanFromSection(toast, "wave", true);
        settings.IsNotificationSoundEnabled =
            settings.AllowNotificationSound = TryGetBooleanFromSection(toast, "ringtone", true);
        var classSettings =
            settings.NotificationProvidersSettings.GetValueOrDefault("08F0D9C3-C770-4093-A3D0-02F3D90C24BC".ToLower()) as ClassNotificationSettings
            ?? new ClassNotificationSettings();
        classSettings.InDoorClassPreparingDeltaTime = classSettings.OutDoorClassPreparingDeltaTime
            = (int)(TryGetDoubleFromSection(toast, "prepare_minutes", 2.0) * 60);
        classSettings.IsClassOnNotificationEnabled = TryGetBooleanFromSection(toast, "attend_class", true);
        classSettings.IsClassOffNotificationEnabled = TryGetBooleanFromSection(toast, "finish_class", true);
        classSettings.IsClassOnPreparingNotificationEnabled = TryGetBooleanFromSection(toast, "prepare_class", true);
        settings.NotificationProvidersSettings["08F0D9C3-C770-4093-A3D0-02F3D90C24BC".ToLower()] = classSettings;
        var afterSchoolSettings =
            settings.NotificationProvidersSettings.GetValueOrDefault("8FBC3A26-6D20-44DD-B895-B9411E3DDC51".ToLower()) as AfterSchoolNotificationProviderSettings
            ?? new AfterSchoolNotificationProviderSettings();
        afterSchoolSettings.IsEnabled = TryGetBooleanFromSection(toast, "after_school", true);
        settings.NotificationProvidersSettings["8FBC3A26-6D20-44DD-B895-B9411E3DDC51".ToLower()] = afterSchoolSettings;
        
        // Time
        var time = ini["Time"];
        settings.IsExactTimeEnabled = TryGetStringFromSection(time, "type", "ntp") == "ntp";
        settings.ExactTimeServer = TryGetStringFromSection(time, "ntp_server", "ntp.aliyun.com");
        settings.TimeOffsetSeconds = TryGetDoubleFromSection(time, "time_offset", 0);
        
        // Date
        var date = ini["Date"];
        settings.SingleWeekStartTime = DateOnly.TryParse(TryGetStringFromSection(date, "start_date", ""), out var d)
            ? d.ToDateTime(TimeOnly.MinValue)
            : DateTime.Now;
        
        // Audio
        var audio = ini["Audio"];
        settings.NotificationSoundVolume = TryGetDoubleFromSection(audio, "volume", 100) / 100.0;
        SetNotificationChannelSound(ClassNotificationProvider.OnClassChannelId,
            TryGetStringFromSection(audio, "attend_class", "attend_class.wav"));
        SetNotificationChannelSound(ClassNotificationProvider.OnBreakingChannelId,
            TryGetStringFromSection(audio, "finish_class", "finish_class.wav"));
        SetNotificationChannelSound(ClassNotificationProvider.PrepareOnClassChannelId,
            TryGetStringFromSection(audio, "prepare_class", "prepare_class.wav"));

        return;
        
        void SetNotificationChannelSound(string channelId, string cwPath)
        {
            var chanelSettings =
                settings.NotificationChannelsNotifySettings.GetValueOrDefault(channelId) ?? new NotificationSettings();
            chanelSettings.IsNotificationSoundEnabled = true;
            chanelSettings.IsSettingsEnabled = true;
            if (!Directory.Exists(Path.Combine(CommonDirectories.AppConfigPath, "ExternalAudios")))
            {
                Directory.CreateDirectory(Path.Combine(CommonDirectories.AppConfigPath, "ExternalAudios"));
            }
            var ciPath = Path.GetFullPath(Path.Combine(CommonDirectories.AppConfigPath, "ExternalAudios", cwPath));
            File.Copy(Path.Combine(root, "audio", cwPath), ciPath, true);
            chanelSettings.NotificationSoundPath = ciPath;
            settings.NotificationChannelsNotifySettings[new Guid(channelId).ToString()] = chanelSettings;
        }
    }

    private void ImportClassWidgetsComponents(string root, IniData ini)
    {
        var profile = new ComponentProfile()
        {
            Lines = [
                new MainWindowLineSettings()
            ]
        };
        var line = profile.Lines[0];
        var widgets =
            ConfigureFileHelper.LoadConfigUnWrapped<CwWidgetsProfile>(Path.Combine(root, "config", "widget.json"));
        foreach (var widget in widgets.Widgets)
        {
            var comp = new ComponentSettings()
            {
                Id = widget switch
                {
                    "widget-weather.ui" => "CA495086-E297-4BEB-9603-C5C1C1A8551E",
                    "widget-time.ui" => "9E1AF71D-8F77-4B21-A342-448787104DD9",
                    "widget-next-activity.ui" => "E7831603-61A0-4180-B51B-54AD75B1A4D3",
                    "widget-countdown-day.ui" => "7C645D35-8151-48BA-B4AC-15017460D994",
                    _ => ""
                }
            };
            if (comp.Id == "")
            {
                continue;
            }
            line.Children.Add(comp);
        }

        var name = $"cw-{Guid.NewGuid()}.json";
        ConfigureFileHelper.SaveConfig(Path.Combine(ComponentsService.ComponentSettingsPath, name), profile);
        var settingsService = IAppHost.GetService<SettingsService>();
        var settings = settingsService.Settings;
        settings.CurrentComponentConfig = Path.GetFileNameWithoutExtension(name);
    }
    
    private async Task PerformClassWidgetsImportAction()
    {
        if (string.IsNullOrWhiteSpace(ViewModel.ImportSourcePath))
        {
            return;
        }
        var r = await new ContentDialog()
        {
            Title = "重启以继续",
            Content = "应用需要重启以完全应用导入操作，要继续吗？",
            PrimaryButtonText = "重启并继续",
            SecondaryButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        }.ShowAsync(TopLevel.GetTopLevel(this));
        if (r != ContentDialogResult.Primary)
        {
            return;
        }
        var root = ViewModel.ImportSourcePath;
        try
        {
            ViewModel.PageIndex = 3;
            await Task.Run(() =>
            {
                var parser = new IniDataParser()
                {
                    Configuration =
                    {
                        SkipInvalidLines = true
                    }
                };
                var ini = parser.Parse(File.ReadAllText(Path.Combine(root, "config.ini")));
                if (ViewModel.IsProfileSelected)
                {
                    ImportClassWidgetsProfile(root, ini);
                    var general = ini["General"];
                    var profileName = "cw_" + TryGetStringFromSection(general, "schedule", "新课表 - 1.json");
                    var settingsService = IAppHost.GetService<SettingsService>();
                    var settings = settingsService.Settings;
                    settings.SelectedProfile = profileName;
                }
                if (ViewModel.IsSettingsSelected)
                {
                    ImportClassWidgetsSettings(root, ini);
                }
                if (ViewModel.IsOtherConfigSelected)
                {
                    ImportClassWidgetsComponents(root, ini);
                }
            });
            AppBase.Current.Restart(["-m", "--importComplete"]);
        }
        catch (Exception e)
        {
            this.ShowErrorToast("导入时发生意外错误", e);
        }
    }

    #endregion

    private void ButtonFinish_OnClick(object? sender, RoutedEventArgs e)
    {
        (TopLevel.GetTopLevel(this) as Window)?.Close();
    }

    private void ButtonNext_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.PageIndex++;
    }

    
}