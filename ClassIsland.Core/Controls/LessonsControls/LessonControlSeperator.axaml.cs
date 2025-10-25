using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ClassIsland.Shared.Abstraction.Models;

namespace ClassIsland.Core.Controls.LessonsControls;

public partial class LessonControlSeparator : LessonControlBase
{
    public static readonly StyledProperty<ILessonControlSettings> LessonControlSettingsProperty = AvaloniaProperty.Register<LessonControlSeparator, ILessonControlSettings>(
        nameof(LessonControlSettings));

    public ILessonControlSettings LessonControlSettings
    {
        get => GetValue(LessonControlSettingsProperty);
        set => SetValue(LessonControlSettingsProperty, value);
    }
    
    public LessonControlSeparator()
    {
        InitializeComponent();
    }
}