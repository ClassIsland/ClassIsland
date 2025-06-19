using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
namespace ClassIsland.ViewModels;

public partial class WeekOffsetSettingsControlViewModel : ObservableObject
{
    /// <remarks> 2-first, 0-based </remarks>
    [ObservableProperty] ObservableCollection<int> _cyclePositionIndexes = [-1, -1, ];
}