using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ClassIsland.Core.Helpers.UI;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.iOS.Services.LiveActivities;

/// <summary>
/// 复用课表的图标表达式解析器，将图标转为原生视图可直接展示的小型 PNG。
/// </summary>
internal sealed class LiveActivityLessonIconRenderer
{
    private string? _lastExpression;
    private string? _lastImage;

    public string? Render(string expression)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (expression == _lastExpression)
        {
            return _lastImage;
        }

        _lastExpression = expression;
        _lastImage = null;
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        try
        {
            var source = IconExpressionHelper.TryParseOrNull(expression);
            if (source == null)
            {
                return null;
            }
            if (source is FAFontIconSource fontSource)
            {
                fontSource.FontSize = 26;
            }

            var icon = new FAIconSourceElement
            {
                IconSource = source,
                Width = 28,
                Height = 28,
                Foreground = new SolidColorBrush(Color.Parse("#8AD5FF"))
            };
            TextElement.SetFontSize(icon, 26);
            icon.Classes.Add("repair-fontsize");
            icon.ApplyStyling();
            icon.Measure(new Size(28, 28));
            icon.Arrange(new Rect(0, 0, 28, 28));

            foreach (var pixels in new[] { 56, 40, 28 })
            {
                using var bitmap = new RenderTargetBitmap(new PixelSize(pixels, pixels),
                    new Vector(pixels / 28.0 * 96, pixels / 28.0 * 96));
                bitmap.Render(icon);
                using var stream = new MemoryStream();
                bitmap.Save(stream);
                var encoded = Convert.ToBase64String(stream.ToArray());
                if (encoded.Length > 1536)
                {
                    continue;
                }

                _lastImage = encoded;
                break;
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Unable to render Live Activity lesson icon: {exception}");
        }

        return _lastImage;
    }
}
