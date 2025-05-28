using Avalonia;
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

    public static readonly StyledProperty<ILessonControlSettings> DefaultLessonControlSettingsProperty = AvaloniaProperty.Register<LessonControlMinimized, ILessonControlSettings>(
        nameof(DefaultLessonControlSettings));
    
    public ILessonControlSettings DefaultLessonControlSettings
    {
        get => GetValue(DefaultLessonControlSettingsProperty);
        set => SetValue(DefaultLessonControlSettingsProperty, value);
    }
}
