using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClassIsland.Models;

namespace ClassIsland.Controls;

public class SfSymbolIcon : Image
{
    public static readonly DependencyProperty DrawingGeometryProperty = DependencyProperty.Register(
        nameof(DrawingGeometry), typeof(Geometry), typeof(SfSymbolIcon), new PropertyMetadata(default(Geometry)));

    public Geometry DrawingGeometry
    {
        get { return (Geometry)GetValue(DrawingGeometryProperty); }
        set { SetValue(DrawingGeometryProperty, value); }
    }

    public static readonly DependencyProperty DrawingClipGeometryProperty = DependencyProperty.Register(
        nameof(DrawingClipGeometry), typeof(Geometry), typeof(SfSymbolIcon), new PropertyMetadata(default(Geometry)));

    public Geometry DrawingClipGeometry
    {
        get { return (Geometry)GetValue(DrawingClipGeometryProperty); }
        set { SetValue(DrawingClipGeometryProperty, value); }
    }

    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(SfSymbolIconKind), typeof(SfSymbolIcon), new PropertyMetadata(default(SfSymbolIconKind),
            (o, args) =>
            {
                if (o is SfSymbolIcon control)
                {
                    control.UpdateIcon();
                }
            }));

    public SfSymbolIconKind Kind
    {
        get { return (SfSymbolIconKind)GetValue(KindProperty); }
        set { SetValue(KindProperty, value); }
    }

    private void UpdateIcon()
    {
        DrawingGeometry = Geometry.Parse(SfSymbolIconData.PathData[Kind]);
        DrawingClipGeometry = Geometry.Parse(SfSymbolIconClipData.PathData[Kind]);
    }
}