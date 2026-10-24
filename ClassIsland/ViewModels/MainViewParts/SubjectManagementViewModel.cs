using System;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.ViewModels.MainViewParts;

internal sealed class SubjectManagementViewModel(IProfileService profileService) : IDisposable
{
    public SyncDictionaryList<Guid, Subject> Subjects { get; } = new(profileService.Profile.Subjects, Guid.NewGuid);

    public void Dispose() => Subjects.Dispose();
}
