using System;
using System.Collections.Generic;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Extensions;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.MainViewParts;

public sealed class SubjectEditViewModel : ObservableObject
{
    private readonly IProfileService _profileService;

    public SubjectEditViewModel(IProfileService profileService, Guid subjectId, bool isNew = false,
        string initialName = "")
    {
        _profileService = profileService;
        SubjectId = subjectId;
        Subject = isNew
            ? new Subject { Name = initialName }
            : profileService.Profile.Subjects.GetValueOrDefault(subjectId) ?? new Subject();
    }

    public Guid SubjectId { get; }
    public bool IsNew => !_profileService.Profile.Subjects.ContainsKey(SubjectId);
    public Subject Subject { get; }

    public void Commit()
    {
        Subject.Name = Subject.Name.Trim();
        var wasNew = IsNew;
        if (wasNew)
        {
            _profileService.Profile.Subjects.Add(SubjectId, Subject);
        }

        _profileService.SaveProfile(() =>
        {
            if (wasNew)
            {
                _profileService.Profile.Subjects.Remove(SubjectId);
            }
        });
    }

    public void Delete()
    {
        var subjects = _profileService.Profile.Subjects;
        if (!subjects.TryGetValue(SubjectId, out var subject))
        {
            return;
        }

        var item = new KeyValuePair<Guid, Subject>(SubjectId, subject);
        var index = subjects.IndexOf(item);
        subjects.RemoveAt(index);
        _profileService.SaveProfile(() => subjects.Insert(index, item));
    }
}
