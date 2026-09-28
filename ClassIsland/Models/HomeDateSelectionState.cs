using System;

namespace ClassIsland.Models;

// Keep browsing state across transient main views without persisting it to the profile.
internal sealed class HomeDateSelectionState
{
    public DateTime? SelectedDate { get; set; }
    public DateTime? LastToday { get; set; }
}
