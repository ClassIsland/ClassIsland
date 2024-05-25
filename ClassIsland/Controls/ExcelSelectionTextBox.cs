using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using unvell.ReoGrid;

namespace ClassIsland.Controls
{
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
    ///     <MyNamespace:ExcelSelectionTextBox/>
    ///
    /// </summary>
    public class ExcelSelectionTextBox : TextBox
    {
        public static readonly DependencyProperty IsSelectingProperty = DependencyProperty.Register(
            nameof(IsSelecting), typeof(bool), typeof(ExcelSelectionTextBox), new PropertyMetadata(default(bool)));

        public bool IsSelecting
        {
            get { return (bool)GetValue(IsSelectingProperty); }
            set { SetValue(IsSelectingProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(RangePosition), typeof(ExcelSelectionTextBox), new PropertyMetadata(RangePosition.Empty));

        public RangePosition Value
        {
            get { return (RangePosition)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty EnterSelectingModeCommandProperty = DependencyProperty.Register(
            nameof(EnterSelectingModeCommand), typeof(ICommand), typeof(ExcelSelectionTextBox), new PropertyMetadata(default(ICommand)));

        public ICommand EnterSelectingModeCommand
        {
            get { return (ICommand)GetValue(EnterSelectingModeCommandProperty); }
            set { SetValue(EnterSelectingModeCommandProperty, value); }
        }

        public static readonly DependencyProperty SelectionPropertyNameProperty = DependencyProperty.Register(
            nameof(SelectionPropertyName), typeof(string), typeof(ExcelSelectionTextBox), new PropertyMetadata(default(string)));

        public string SelectionPropertyName
        {
            get { return (string)GetValue(SelectionPropertyNameProperty); }
            set { SetValue(SelectionPropertyNameProperty, value); }
        }

        public static readonly DependencyProperty IsCurrentSelectingProperty = DependencyProperty.Register(
            nameof(IsCurrentSelecting), typeof(bool), typeof(ExcelSelectionTextBox), new PropertyMetadata(default(bool)));

        public bool IsCurrentSelecting
        {
            get { return (bool)GetValue(IsCurrentSelectingProperty); }
            set { SetValue(IsCurrentSelectingProperty, value); }
        }

        static ExcelSelectionTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ExcelSelectionTextBox), new FrameworkPropertyMetadata(typeof(ExcelSelectionTextBox)));
        }

        public ExcelSelectionTextBox()
        {
            
        }

        public event EventHandler? OnEnterSelecting;

        public event EventHandler? OnExitSelecting;

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == IsSelectingProperty && (bool)e.NewValue == false)
            {
                IsCurrentSelecting = false;
                OnExitSelecting?.Invoke(this, EventArgs.Empty);
            }
            base.OnPropertyChanged(e);
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            SelectAll();
            if (IsSelecting)
            {
                return;
            }
            //IsSelecting = true;
            IsCurrentSelecting = true;
            OnEnterSelecting?.Invoke(this, EventArgs.Empty);
            EnterSelectingModeCommand?.Execute(SelectionPropertyName);
        }
    }
}
