using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Automation.Triggers;

public partial class UriTriggerSettings : ObservableObject
{
    [ObservableProperty] private string _uriSuffix = "";
}