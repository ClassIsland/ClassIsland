using System.Windows;

using ClassIsland.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;

namespace ClassIsland.Views;

/// <summary>
/// FeatureDebugWindow.xaml 的交互逻辑
/// </summary>
public partial class FeatureDebugWindow : MyWindow
{
    public ILessonsService LessonsService { get; }

    public IProfileService ProfileService { get; }

    public FeatureDebugWindow(ILessonsService lessonsService, IProfileService profileService)
    {
        DataContext = this;
        LessonsService = lessonsService;
        ProfileService = profileService;
        InitializeComponent();
    }

    private void ButtonPlayEffect_OnClick(object sender, RoutedEventArgs e)
    {
        RippleEffect.Play();
    }
}