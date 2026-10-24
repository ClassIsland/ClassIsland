using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace ClassIsland.ViewModels.MainViewParts;

internal sealed partial class SubjectPickerViewModel : ObservableObject, IDisposable
{
    private readonly SyncDictionaryList<Guid, Subject> _subjects;
    private readonly IDisposable _subjectsSubscription;
    private readonly ReadOnlyObservableCollection<KeyValuePair<Guid, Subject>> _filteredSubjects;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CreateButtonText))]
    [NotifyPropertyChangedFor(nameof(CanCreateSubject))]
    private string _searchText = "";

    public SubjectPickerViewModel(IProfileService profileService)
    {
        _subjects = new SyncDictionaryList<Guid, Subject>(profileService.Profile.Subjects, Guid.NewGuid);
        var reordered = Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => _subjects.List.CollectionChanged += handler,
                handler => _subjects.List.CollectionChanged -= handler)
            .Where(change => change.EventArgs.Action == NotifyCollectionChangedAction.Move)
            .Select(_ => Unit.Default);
        var filter = this.WhenAnyValue(x => x.SearchText)
            .Select(text => new Func<KeyValuePair<Guid, Subject>, bool>(pair =>
            {
                var query = text.Trim();
                var subject = pair.Value;
                return string.IsNullOrWhiteSpace(query) ||
                       subject.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       subject.Initial.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       subject.TeacherName.Contains(query, StringComparison.OrdinalIgnoreCase);
            }));
        _subjectsSubscription = _subjects.List
            .ToObservableChangeSet()
            .AutoRefreshOnObservable(pair => pair.Value.WhenAnyPropertyChanged())
            .Filter(filter)
            // 列表过滤会丢弃移动通知，因此按档案顺序重新排列过滤结果。
            .Sort(Comparer<KeyValuePair<Guid, Subject>>.Create((left, right) =>
                    _subjects.List.IndexOf(left).CompareTo(_subjects.List.IndexOf(right))),
                resort: reordered)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _filteredSubjects)
            .Subscribe(_ => OnPropertyChanged(nameof(CanCreateSubject)));
    }

    public ReadOnlyObservableCollection<KeyValuePair<Guid, Subject>> Subjects => _filteredSubjects;
    public string CreateButtonText => $"创建科目“{SearchText.Trim()}”";
    public bool CanCreateSubject => !string.IsNullOrWhiteSpace(SearchText) &&
                                    !_subjects.List.Any(pair => string.Equals(
                                        pair.Value.Name, SearchText.Trim(), StringComparison.OrdinalIgnoreCase));

    public void Dispose()
    {
        _subjectsSubscription.Dispose();
        _subjects.Dispose();
    }
}
