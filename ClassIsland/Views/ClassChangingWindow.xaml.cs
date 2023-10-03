using ClassIsland.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
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
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.ViewModels;

namespace ClassIsland.Views;

/// <summary>
/// ClassChangingWindow.xaml 的交互逻辑
/// </summary>
public partial class ClassChangingWindow : MyWindow
{
    public ClassChangingViewModel ViewModel { get; } = new();

    public ProfileService ProfileService { get; } = App.GetService<ProfileService>();

    public static readonly DependencyProperty ClassPlanProperty = DependencyProperty.Register(
        nameof(ClassPlan), typeof(ClassPlan), typeof(ClassChangingWindow), new PropertyMetadata(default(ClassPlan)));

    public ClassPlan ClassPlan
    {
        get { return (ClassPlan)GetValue(ClassPlanProperty); }
        set { SetValue(ClassPlanProperty, value); }
    }

    public ClassChangingWindow()
    {
        DataContext = this;
        InitializeComponent();
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.IsAutoNextStep)
            return;
        ViewModel.IsAutoNextStep = true;
        ViewModel.SlideIndex = 1;
    }

    private void ButtonPrev_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SlideIndex = 0;
    }

    private void ButtonNext_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SlideIndex = 1;
    }
}