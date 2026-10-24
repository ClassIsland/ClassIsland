using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.MainViewParts;

internal sealed class ImportExportViewModel : ObservableObject
{
    public ImportExportViewModel()
    {
        foreach (var group in IProfileTransferService.Providers
                     .OrderBy(x => x.Type)
                     .GroupBy(x => x.Type))
        {
            ProviderGroups.Add(new ProviderGroup(ProviderGroups.Count > 0, group.ToList()));
        }
    }

    public ObservableCollection<ProviderGroup> ProviderGroups { get; } = [];

    public sealed record ProviderGroup(bool HasSeparator, IReadOnlyList<ProfileTransferProviderInfo> Providers);
}
