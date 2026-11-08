using System;
using Avalonia.Media;
using ClassIsland.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClassIsland.ViewModels;

public partial class DevPortalViewModel
{
    [ObservableProperty] private string _fontProbeText = "尚未探测";
    [ObservableProperty] private int _minimumFontWeight = 1;
    [ObservableProperty] private int _maximumFontWeight = 1000;
    private int _selectedFontWeight = 400;

    public int SelectedFontWeight
    {
        get => _selectedFontWeight;
        set
        {
            if (SetProperty(ref _selectedFontWeight, Math.Clamp(value, MinimumFontWeight, MaximumFontWeight)))
            {
                OnPropertyChanged(nameof(PreviewFontWeight));
            }
        }
    }

    public FontWeight PreviewFontWeight => (FontWeight)SelectedFontWeight;

    [RelayCommand]
    private void RefreshFontProbe()
    {
        var result = DefaultFontProbe.Read();
        FontProbeText = result.DisplayText;
        var minimum = 1;
        var maximum = 1000;
        if (result.WeightAxis is { } axis)
        {
            minimum = (int)Math.Clamp(Math.Ceiling(axis.Minimum), 1, 1000);
            maximum = (int)Math.Clamp(Math.Floor(axis.Maximum), 1, 1000);
            if (minimum > maximum)
            {
                minimum = 1;
                maximum = 1000;
            }
        }

        // Expand first so two-way bound controls never observe an inverted range.
        MinimumFontWeight = Math.Min(MinimumFontWeight, minimum);
        MaximumFontWeight = Math.Max(MaximumFontWeight, maximum);
        MinimumFontWeight = minimum;
        MaximumFontWeight = maximum;
        SelectedFontWeight = SelectedFontWeight;
    }

    [RelayCommand]
    private void ResetFontWeight() => SelectedFontWeight = 400;
}
