using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland;

[INotifyPropertyChanged]
public partial class SourceAdapter<T>
{
    [ObservableProperty] private T? model;
}