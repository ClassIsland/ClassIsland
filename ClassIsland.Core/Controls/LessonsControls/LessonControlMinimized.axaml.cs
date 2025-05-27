using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;








using ClassIsland.Shared.Abstraction.Models;

namespace ClassIsland.Core.Controls.LessonsControls;

/// <summary>
/// LessonControlMinimized.xaml 的交互逻辑
/// </summary>
public partial class LessonControlMinimized : LessonControlBase
{
    /// <inheritdoc />
    public LessonControlMinimized()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty DefaultLessonControlSettingsProperty = DependencyProperty.Register(
        nameof(DefaultLessonControlSettings), typeof(ILessonControlSettings), typeof(LessonControlMinimized), new PropertyMetadata(default(ILessonControlSettings)));

    public ILessonControlSettings DefaultLessonControlSettings
    {
        get { return (ILessonControlSettings)GetValue(DefaultLessonControlSettingsProperty); }
        set { SetValue(DefaultLessonControlSettingsProperty, value); }
    }
}
