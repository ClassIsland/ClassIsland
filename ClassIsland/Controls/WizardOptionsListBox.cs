using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClassIsland.Controls;

/// <summary>
/// 按照步骤 1a 或 1b 操作，然后执行步骤 2 以在 XAML 文件中使用此自定义控件。
///
/// 步骤 1a) 在当前项目中存在的 XAML 文件中使用该自定义控件。
/// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根
/// 元素中:
///
///     xmlns:MyNamespace="clr-namespace:ClassIsland.Controls"
///
///
/// 步骤 1b) 在其他项目中存在的 XAML 文件中使用该自定义控件。
/// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根
/// 元素中:
///
///     xmlns:MyNamespace="clr-namespace:ClassIsland.Controls;assembly=ClassIsland.Controls"
///
/// 您还需要添加一个从 XAML 文件所在的项目到此项目的项目引用，
/// 并重新生成以避免编译错误:
///
///     在解决方案资源管理器中右击目标项目，然后依次单击
///     “添加引用”->“项目”->[浏览查找并选择此项目]
///
///
/// 步骤 2)
/// 继续操作并在 XAML 文件中使用控件。
///
///     <MyNamespace:WizardOptionsListBox/>
///
/// </summary>
public class WizardOptionsListBox : ListBox
{
    public static readonly DependencyProperty InvokeCommandProperty = DependencyProperty.Register(
        nameof(InvokeCommand), typeof(ICommand), typeof(WizardOptionsListBox), new PropertyMetadata(default(ICommand)));

    public ICommand InvokeCommand
    {
        get { return (ICommand)GetValue(InvokeCommandProperty); }
        set { SetValue(InvokeCommandProperty, value); }
    }

    public static readonly DependencyProperty InvokeCommandParameterProperty = DependencyProperty.Register(
        nameof(InvokeCommandParameter), typeof(object), typeof(WizardOptionsListBox), new PropertyMetadata(default(object)));

    public object InvokeCommandParameter
    {
        get { return GetValue(InvokeCommandParameterProperty); }
        set { SetValue(InvokeCommandParameterProperty, value); }
    }

    static WizardOptionsListBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(WizardOptionsListBox), new FrameworkPropertyMetadata(typeof(WizardOptionsListBox)));
    }
}