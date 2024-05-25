using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Profile;

public class Profile : ObservableRecipient
{
    private string _name = "";
    private ObservableDictionary<string, TimeLayout> _timeLayouts = new();
    private ObservableDictionary<string, ClassPlan> _classPlans = new();
    private ObservableDictionary<string, Subject> _subjects = new();
    private bool _isOverlayClassPlanEnabled = false;
    private string? _overlayClassPlanId = null;
    private ObservableCollection<Subject> _editingSubjects = new();
    private string? _tempClassPlanId;
    private DateTime _tempClassPlanSetupTime = DateTime.Now;

    public Profile()
    {
        Subjects.CollectionChanged += SubjectsOnCollectionChanged;
        PropertyChanging += OnPropertyChanging;
        PropertyChanged += OnPropertyChanged;
        UpdateEditingSubjects();
    }

    private void OnPropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (e.PropertyName == nameof(Subjects))
        {
            Subjects.CollectionChanged -= SubjectsOnCollectionChanged;
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Subjects))
        {
            Subjects.CollectionChanged += SubjectsOnCollectionChanged;
            UpdateEditingSubjects();
        }
    }

    public void OverwriteAllClassPlanSubject(string timeLayoutId, TimeLayoutItem timePoint, string subjectId)
    {
        foreach (var classPlan in from i in ClassPlans where i.Value.TimeLayoutId == timeLayoutId select i.Value)
        {
            classPlan.RefreshClassesList();
            foreach (var i in from i in classPlan.Classes where i.CurrentTimeLayoutItem == timePoint select i)
            {
                i.SubjectId = subjectId;
            }
        }
    }

    private void UpdateEditingSubjects(NotifyCollectionChangedEventArgs? e=null)
    {
        if (e != null)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            EditingSubjects.CollectionChanged -= EditingSubjectsOnCollectionChanged;
            EditingSubjects = new ObservableCollection<Subject>(from i in Subjects select i.Value);
            EditingSubjects.CollectionChanged += EditingSubjectsOnCollectionChanged;
        }
    }

    private void EditingSubjectsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Console.WriteLine($"{e.Action} {e.NewItems} {e.OldItems}");
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems == null)
                {
                    break;
                }
                foreach (var i in e.NewItems)
                {
                    Subjects[Guid.NewGuid().ToString()] = (Subject)i;
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems == null)
                {
                    break;
                }
                foreach (var i in e.OldItems)
                {
                    foreach (var k in Subjects.Where(k => k.Value == i))
                    {
                        Subjects.Remove(k.Key);
                        break;
                    }
                }

                //Subjects = ConfigureFileHelper.CopyObject(Subjects);
                break;
            case NotifyCollectionChangedAction.Replace:
                break;
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Reset:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        foreach (var i in Subjects)
        {
            Console.WriteLine($"{i.Key} {i.Value.Name}" );
        }
    }

    private void SubjectsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateEditingSubjects(e);
    }

    public void NotifyPropertyChanged(string propertyName)
    {
        OnPropertyChanged(propertyName);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (value == _name) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    public ObservableDictionary<string, TimeLayout> TimeLayouts
    {
        get => _timeLayouts;
        set
        {
            if (Equals(value, _timeLayouts)) return;
            _timeLayouts = value;
            OnPropertyChanged();
        }
    }

    public ObservableDictionary<string, ClassPlan> ClassPlans
    {
        get => _classPlans;
        set
        {
            if (Equals(value, _classPlans)) return;
            _classPlans = value;
            OnPropertyChanged();
            _classPlans.CollectionChanged += delegate(object? sender, NotifyCollectionChangedEventArgs args)
            {
                RefreshTimeLayouts();
            };

            RefreshTimeLayouts();
        }
    }

    public void RefreshTimeLayouts()
    {
        foreach (var i in _classPlans)
        {
            i.Value.TimeLayouts = TimeLayouts;
        }
    }

    public ObservableDictionary<string, Subject> Subjects
    {
        get => _subjects;
        set
        {
            if (Equals(value, _subjects)) return;
            _subjects = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public ObservableCollection<Subject> EditingSubjects
    {
        get => _editingSubjects;
        set
        {
            if (Equals(value, _editingSubjects)) return;
            _editingSubjects = value;
            OnPropertyChanged();
        }
    }

    public bool IsOverlayClassPlanEnabled
    {
        get => _isOverlayClassPlanEnabled;
        set
        {
            if (value == _isOverlayClassPlanEnabled) return;
            _isOverlayClassPlanEnabled = value;
            OnPropertyChanged();
        }
    }

    public string? OverlayClassPlanId
    {
        get => _overlayClassPlanId;
        set
        {
            if (value == _overlayClassPlanId) return;
            _overlayClassPlanId = value;
            OnPropertyChanged();
        }
    }

    public string? TempClassPlanId
    {
        get => _tempClassPlanId;
        set
        {
            if (value == _tempClassPlanId) return;
            _tempClassPlanId = value;
            OnPropertyChanged();
        }
    }

    public DateTime TempClassPlanSetupTime
    {
        get => _tempClassPlanSetupTime;
        set
        {
            if (value.Equals(_tempClassPlanSetupTime)) return;
            _tempClassPlanSetupTime = value;
            OnPropertyChanged();
        }
    }
}