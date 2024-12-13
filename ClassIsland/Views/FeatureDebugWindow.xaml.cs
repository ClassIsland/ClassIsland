using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClassIsland.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers;

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

    private async void ButtonTestFakeLoading_OnClick(object sender, RoutedEventArgs e)
    {
        LoadingMask.StartFakeLoading();
        await Task.Delay(TimeSpan.FromSeconds(5));
        LoadingMask.FinishFakeLoading();

    }

    private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!e.Handled)
        {
            // ListView拦截鼠标滚轮事件
            e.Handled = true;

            // 激发一个鼠标滚轮事件，冒泡给外层ListView接收到
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            var parent = ((System.Windows.Controls.Control)sender).Parent as UIElement;
            if (parent != null)
            {
                parent.RaiseEvent(eventArg);
            }
        }
    }

    private void TextBoxMarkdown_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        MarkdownReader.Document = MarkdownConvertHelper.ConvertMarkdown(TextBoxMarkdown.Text);
    }
}