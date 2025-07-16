using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

namespace ClassIsland.Controls;

public partial class OobeIntroAnimationControl : UserControl
{
    
    public OobeIntroAnimationControl()
    {
        InitializeComponent();
    }

    private async Task PlayAnimationPhase1()
    {
        Classes.Add("anim");
        var children1 = Rects.Children.ToList();
        var children2 = Texts.Children.ToList();
        var durationMs = 500;
        var count = Math.Min(children1.Count, children2.Count);
        var index = 0;

        Task? last = null;
        for (int i = 0; i < count; i++)
        {
            var delay = Math.Sin((1.0 * (i + 2) / (count + 2)) * (Math.PI / 2)) * durationMs / count;
            Console.WriteLine(delay);
            var c1 = children1[i];
            var c2 = children2[i];
            var anim1 = BuildAnimation1(delay * 9, i == 6);
            var anim2 = BuildAnimation2(delay * 9);
            _ = anim1.RunAsync(c1);
            var t = anim2.RunAsync(c2);
            c1.Classes.Add("anim");
            c2.Classes.Add("anim");
            if (i == count - 3)
            {
                last = t;
            }
            await Task.Delay(
                TimeSpan.FromMilliseconds(delay));
            
        }

        if (last != null)
        {
            await last;
        }
        Texts.Classes.Add("anim");
        
        return;

        Animation BuildAnimation1(double timeMs, bool willHide)
        {
            var animation = new Animation()
            {
                FillMode = FillMode.Both,
                Duration = TimeSpan.FromMilliseconds(timeMs * 1.5 + 750),
                Children =
                {
                    new KeyFrame()
                    {
                        Setters =
                        {
                            new Setter(OpacityProperty, 0.0),
                            new Setter(TranslateTransform.YProperty, 50.0),
                        },
                        KeyTime = TimeSpan.FromMilliseconds(0)
                    },
                    new KeyFrame()
                    {
                        KeyTime = TimeSpan.FromMilliseconds(timeMs),
                        Setters =
                        {
                            new Setter(OpacityProperty, 1.0),
                            new Setter(TranslateTransform.YProperty, 0.0),
                        },
                        KeySpline = KeySpline.Parse("0.25, 1, 0.5, 1", CultureInfo.CurrentUICulture)
                    },
                    new KeyFrame()
                    {
                        KeyTime = TimeSpan.FromMilliseconds(timeMs + 1),
                        Setters =
                        { 
                            new Setter(OpacityProperty, willHide ? 0.0 : 1.0)
                        }
                    },
                    new KeyFrame()
                    {
                        KeyTime = TimeSpan.FromMilliseconds(timeMs + 750),
                        Setters =
                        { 
                            new Setter(OpacityProperty, willHide ? 0.0 : 1.0),
                            new Setter(Rotate3DTransform.AngleXProperty, 0.0)
                        }
                    },
                    new KeyFrame()
                    {
                        Cue = new Cue(1.0),
                        Setters =
                        {
                            new Setter(OpacityProperty, 0.0),
                            new Setter(Rotate3DTransform.AngleXProperty, 90.0)
                        },
                        KeySpline = KeySpline.Parse("0.32, 0, 0.67, 0", CultureInfo.CurrentUICulture)
                    }
                    
                }
            };

            return animation;
        }
        
        Animation BuildAnimation2(double timeMs)
        {
            var animation = new Animation()
            {
                FillMode = FillMode.Both,
                Duration = TimeSpan.FromMilliseconds(timeMs * 2 + 750),
                Children =
                {
                    new KeyFrame()
                    {
                        KeyTime = TimeSpan.FromMilliseconds(timeMs * 1.5 + 750),
                        Setters =
                        {
                            new Setter(Rotate3DTransform.AngleXProperty, -90.0),
                            new Setter(OpacityProperty, 0.0)
                        }
                    },
                    new KeyFrame()
                    {
                        Cue = new Cue(1.0),
                        Setters =
                        {
                            new Setter(Rotate3DTransform.AngleXProperty, 0.0),
                            new Setter(OpacityProperty, 1.0)
                        },
                        KeySpline = KeySpline.Parse("0.33, 1, 0.68, 1", CultureInfo.CurrentUICulture)
                    }
                    
                }
            };

            return animation;
        }
    }

    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        await PlayAnimationPhase1();
    }
}