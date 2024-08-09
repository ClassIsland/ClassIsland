using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;

namespace ClassIsland.Views;

/// <summary>
/// ClassPlanDetailsWindow.xaml 的交互逻辑
/// </summary>
public partial class ClassPlanDetailsWindow
{
    public IProfileService ProfileService { get; }

    public ClassPlanDetailsViewModel ViewModel { get; } = new();

    public ClassPlanDetailsWindow(IProfileService profileService)
    {
        ProfileService = profileService;
        InitializeComponent();
    }
}