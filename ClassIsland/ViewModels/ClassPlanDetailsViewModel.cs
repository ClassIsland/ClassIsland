using System.Collections.ObjectModel;
using ClassIsland.Core.Attributes;
using ClassIsland.Models;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class ClassPlanDetailsViewModel : ObservableRecipient
{
    private ClassPlan _classPlan = new();
    private ObservableCollection<LessonDetails> _classes = new ObservableCollection<LessonDetails>();
    private AttachedSettingsControlInfo? _selectedControlInfo;
    private AttachedSettingsControlInfo? _displayControlInfo;
    private LessonDetails? _selectedLesson;
    private ObservableCollection<AttachedSettingsNodeWithState> _nodes = new();
    private string _summary = "";

    public ClassPlan ClassPlan
    {
        get => _classPlan;
        set
        {
            if (Equals(value, _classPlan)) return;
            _classPlan = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<LessonDetails> Classes
    {
        get => _classes;
        set
        {
            if (Equals(value, _classes)) return;
            _classes = value;
            OnPropertyChanged();
        }
    }

    public AttachedSettingsControlInfo? SelectedControlInfo
    {
        get => _selectedControlInfo;
        set
        {
            if (Equals(value, _selectedControlInfo)) return;
            _selectedControlInfo = value;
            OnPropertyChanged();
        }
    }

    public AttachedSettingsControlInfo? DisplayControlInfo
    {
        get => _displayControlInfo;
        set
        {
            if (Equals(value, _displayControlInfo)) return;
            _displayControlInfo = value;
            OnPropertyChanged();
        }
    }

    public LessonDetails? SelectedLesson
    {
        get => _selectedLesson;
        set
        {
            if (Equals(value, _selectedLesson)) return;
            _selectedLesson = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<AttachedSettingsNodeWithState> Nodes
    {
        get => _nodes;
        set
        {
            if (Equals(value, _nodes)) return;
            _nodes = value;
            OnPropertyChanged();
        }
    }

    public string Summary
    {
        get => _summary;
        set
        {
            if (value == _summary) return;
            _summary = value;
            OnPropertyChanged();
        }
    }
}