using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class DataTransferViewModel : ObservableRecipient
{
    [ObservableProperty] private string _importDescription = "";
    [ObservableProperty] private int _pageIndex = 0;
    [ObservableProperty] private Func<Task>? _browseAction;
    [ObservableProperty] private Func<Task>? _performImportAction;
    [ObservableProperty] private string _importSourcePath = "";
}