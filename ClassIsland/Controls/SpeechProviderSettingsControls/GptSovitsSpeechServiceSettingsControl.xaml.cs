using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Shared.Helpers;
using CommunityToolkit.Mvvm.Input;

using GptSoVitsSpeechSettingsList = System.Collections.ObjectModel.ObservableCollection<ClassIsland.Models.GptSoVitsSpeechSettings>;


namespace ClassIsland.Controls.SpeechProviderSettingsControls;

/// <summary>
/// GptSovitsSpeechServiceSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class GptSovitsSpeechServiceSettingsControl : INotifyPropertyChanged
{
    public SettingsService SettingsService { get; }

    public GptSoVitsSpeechSettingsList GptSoVitsSpeechSettingsPresets { get; }

    public static readonly string GptSoVitsSettingsPresetsPath = System.IO.Path.Combine(App.AppConfigPath,
        "GptSovitsSettingsPresets.json");

    public static readonly GptSoVitsSpeechSettingsList InternalGptSoVitsSpeechPresets = [
        new GptSoVitsSpeechSettings
        {
            PresetName = "派蒙（原神）",
            GptSoVitsVoiceName = "paimon",
            IsInternal = true,
            GptSoVitsServerIp = "paimon.tts.wez.ink",
            GptSoVitsBatchSize = 5,
            GptSoVitsPort = "80",
            GptSoVitsPromptText = "既然罗莎莉亚说足迹上有元素力，用元素视野应该能很清楚地看到吧。",
            GptSoVitsRefAudioPath = "template_audio/paimon.wav"
        },
        new GptSoVitsSpeechSettings
        {
            PresetName = "可莉（原神）",
            GptSoVitsVoiceName = "klee",
            IsInternal = true,
            GptSoVitsServerIp = "klee.tts.wez.ink",
            GptSoVitsBatchSize = 5,
            GptSoVitsPort = "80",
            GptSoVitsPromptText = "买东西那天还有一个人一起帮着看了款式，那个人好像叫",
            GptSoVitsRefAudioPath = "template_audio/klee.wav"
        },
        new GptSoVitsSpeechSettings
        {
            PresetName = "神里绫华（原神）",
            GptSoVitsVoiceName = "ayaka",
            IsInternal = true,
            GptSoVitsServerIp = "ayaka.tts.wez.ink",
            GptSoVitsBatchSize = 5,
            GptSoVitsPort = "80",
            GptSoVitsPromptText = "这里有别于神里家的布景，移步之间，处处都有新奇感。",
            GptSoVitsRefAudioPath = "template_audio/ayaka.wav"
        },
        new GptSoVitsSpeechSettings
        {
            PresetName = "爱莉希雅（崩坏3）",
            GptSoVitsVoiceName = "elysia",
            IsInternal = true,
            GptSoVitsServerIp = "elysia.tts.wez.ink",
            GptSoVitsBatchSize = 5,
            GptSoVitsPort = "80",
            GptSoVitsPromptText = "他这么向我说道，悲剧并非终结，而是希望的起始。",
            GptSoVitsRefAudioPath = "template_audio/elysia.wav"
        },
        new GptSoVitsSpeechSettings
        {
            PresetName = "钟离（原神）",
            GptSoVitsVoiceName = "zhongli",
            IsInternal = true,
            GptSoVitsServerIp = "zhongli.tts.wez.ink",
            GptSoVitsBatchSize = 5,
            GptSoVitsPort = "80",
            GptSoVitsPromptText = "无事逢客休，席上校两棋…我们开局吧。",
            GptSoVitsRefAudioPath = "template_audio/zhongli.wav"
        },
        new GptSoVitsSpeechSettings
        {
            PresetName = "流萤（崩坏：星穹铁道）",
            GptSoVitsVoiceName = "firefly",
            IsInternal = true,
            GptSoVitsServerIp = "firefly.tts.wez.ink",
            GptSoVitsBatchSize = 5,
            GptSoVitsPort = "80",
            GptSoVitsPromptText = "因为你身上别着星穹列车的徽章呀，我在大银幕上见过！",
            GptSoVitsRefAudioPath = "template_audio/firefly.wav"
        },
        new GptSoVitsSpeechSettings
        {
            PresetName = "高考听力男声",
            GptSoVitsVoiceName = "highexam-male",
            IsInternal = true,
            GptSoVitsServerIp = "highexam-male.tts.wez.ink",
            GptSoVitsBatchSize = 5,
            GptSoVitsPort = "80",
            GptSoVitsPromptText = "回答听力部分时，请先将答案标在试卷上。",
            GptSoVitsRefAudioPath = "template_audio/highexam-male.wav"
        }
        ,
        new GptSoVitsSpeechSettings
        {
            PresetName = "三月七（崩坏：星穹铁道）",
            GptSoVitsVoiceName = "march7th",
            IsInternal = true,
            GptSoVitsServerIp = "march7th.tts.wez.ink",
            GptSoVitsBatchSize = 5,
            GptSoVitsPort = "80",
            GptSoVitsPromptText = "名字是我自己取的，大家也叫我三月、小三月…你呢？你想叫我什么？",
            GptSoVitsRefAudioPath = "template_audio/march7th.wav"
        }
        ];

    private GptSoVitsSpeechSettings? _selectedGptSoVitsSpeechPreset;

    public GptSoVitsSpeechSettings? SelectedGptSoVitsSpeechPreset
    {
        get => _selectedGptSoVitsSpeechPreset;
        set => SetField(ref _selectedGptSoVitsSpeechPreset, value);
    }

    public GptSovitsSpeechServiceSettingsControl(SettingsService settingsService)
    {
        SettingsService = settingsService;
        InitializeComponent();

        GptSoVitsSpeechSettingsPresets =
            ConfigureFileHelper.LoadConfig<GptSoVitsSpeechSettingsList>(GptSoVitsSettingsPresetsPath);
        foreach (var preset in GptSoVitsSpeechSettingsPresets.Where(x => x.IsInternal).ToList())
        {
            GptSoVitsSpeechSettingsPresets.Remove(preset);
        }

        foreach (var preset in InternalGptSoVitsSpeechPresets)
        {
            GptSoVitsSpeechSettingsPresets.Insert(0, preset);
        }

        ConfigureFileHelper.SaveConfig(GptSoVitsSettingsPresetsPath, GptSoVitsSpeechSettingsPresets);
        GptSoVitsSpeechSettingsPresets.CollectionChanged += (_, _) => ConfigureFileHelper.SaveConfig(GptSoVitsSettingsPresetsPath, GptSoVitsSpeechSettingsPresets);
    }

    private void ButtonSaveGptSovitsPreset_OnClick(object sender, RoutedEventArgs e)
    {
        SaveGptSovitsPreset();
    }

    private void ButtonOpenGptSovitsPresetsDrawer_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("SpeechPresets");
    }

    [RelayCommand]
    private void LoadGptSoVitsPreset(GptSoVitsSpeechSettings settings)
    {
        SettingsService.Settings.GptSoVitsSpeechSettings = ConfigureFileHelper.CopyObject(settings);
    }

    [RelayCommand]
    private void OverwriteGptSoVitsPreset(GptSoVitsSpeechSettings settings)
    {
        GptSoVitsSpeechSettingsPresets.Insert(GptSoVitsSpeechSettingsPresets.IndexOf(settings), ConfigureFileHelper.CopyObject(SettingsService.Settings.GptSoVitsSpeechSettings));
        GptSoVitsSpeechSettingsPresets.Remove(settings);
    }

    [RelayCommand]
    private void DuplicateGptSoVitsPreset(GptSoVitsSpeechSettings settings)
    {
        var newPreset = ConfigureFileHelper.CopyObject(settings);
        newPreset.IsInternal = false;
        GptSoVitsSpeechSettingsPresets.Add(newPreset);
        SelectedGptSoVitsSpeechPreset = newPreset;
    }

    [RelayCommand]
    private void RemoveGptSoVitsPreset(GptSoVitsSpeechSettings settings)
    {
        GptSoVitsSpeechSettingsPresets.Remove(settings);
    }

    private void DataGridPresets_OnBeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
    {
        if (e.Row.Item is GptSoVitsSpeechSettings settings)
        {
            e.Cancel = settings.IsInternal;
        }
    }

    private void ButtonSaveGptSovitsPreset2_OnClick(object sender, RoutedEventArgs e)
    {
        SaveGptSovitsPreset();
        OpenDrawer("SpeechPresets");
    }

    private void SaveGptSovitsPreset()
    {
        var newPreset = ConfigureFileHelper.CopyObject(SettingsService.Settings.GptSoVitsSpeechSettings);
        newPreset.IsInternal = false;
        GptSoVitsSpeechSettingsPresets.Add(newPreset);
        SelectedGptSoVitsSpeechPreset = newPreset;
    }

    private void OpenDrawer(string key)
    {
        var o = FindResource(key);
        if (o is FrameworkElement e)
        {
            e.DataContext = this;
        }
        SettingsPageBase.OpenDrawerCommand.Execute(o);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}