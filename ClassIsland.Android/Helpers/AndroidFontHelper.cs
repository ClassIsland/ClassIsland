using System.Globalization;
using Avalonia.Media;
using ClassIsland.Core;
using SkiaSharp;

namespace ClassIsland.Android.Helpers;

internal static class AndroidFontHelper
{
    private const string ProbeText = "中文课表设置 悲剧并非终结，而是希望的起始。ABC0123456789";
    private static readonly char[] ChineseProbeCharacters =
        ProbeText.Where(character => !char.IsAscii(character)).Distinct().ToArray();

    private static readonly List<FontFallback> Fallbacks =
    [
        new FontFallback { FontFamily = AppBase.FluentIconsFontFamily }
    ];

    private static readonly Lazy<FontManagerOptions> Options = new(CreateOptions);
    private static readonly Lazy<bool> FallbacksInitialized = new(CacheFallbacks);

    internal static FontManagerOptions GetOptions() => Options.Value;

    internal static void InitializeFallbacks() => _ = FallbacksInitialized.Value;

    private static CultureInfo FontCulture => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh"
        ? CultureInfo.CurrentUICulture
        : CultureInfo.GetCultureInfo("zh-Hans");

    private static FontManagerOptions CreateOptions()
    {
        var options = new FontManagerOptions { FontFallbacks = Fallbacks };
        try
        {
            if (CoversProbe(SKTypeface.Default))
            {
                Fallbacks.Add(new FontFallback { FontFamily = FontFamily.Default });
                return options;
            }

            var fontManager = SKFontManager.Default;
            var checkedFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var character in ChineseProbeCharacters)
            {
                var matched = fontManager.MatchCharacter(null, SKFontStyle.Normal, [FontCulture.Name], character);
                if (matched == null || !checkedFamilies.Add(matched.FamilyName) || !CoversProbe(matched))
                {
                    continue;
                }

                // Android fallback fonts may be anonymous: matching a character does not guarantee
                // that loading the returned family name will resolve to the same font.
                var named = fontManager.MatchFamily(matched.FamilyName, SKFontStyle.Normal);
                if (named == null || string.IsNullOrWhiteSpace(named.FamilyName) || !CoversProbe(named))
                {
                    continue;
                }

                options.DefaultFamilyName = named.FamilyName;
                Fallbacks.Add(new FontFallback { FontFamily = new FontFamily(named.FamilyName) });
                global::Android.Util.Log.Info(nameof(AndroidFontHelper),
                    $"使用支持中文的系统默认字体：{named.FamilyName}");
                return options;
            }

            global::Android.Util.Log.Warn(nameof(AndroidFontHelper),
                "未找到可按名称加载且覆盖中文的系统字体，将尝试系统字符回退。");
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Warn(nameof(AndroidFontHelper), exception.ToString());
        }

        return options;
    }

    private static bool CoversProbe(SKTypeface typeface)
    {
        // Typefaces returned by the system font manager are shared; only dispose our font wrapper.
        using var font = new SKFont(typeface);
        return font.ContainsGlyphs(ProbeText);
    }

    private static bool CacheFallbacks()
    {
        try
        {
            var fontManager = FontManager.Current;
            if (Fallbacks.Count > 1)
            {
                return true;
            }

            var families = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var weight in new[] { FontWeight.Normal, FontWeight.SemiBold, FontWeight.Bold })
            {
                foreach (var character in ChineseProbeCharacters)
                {
                    // Use Avalonia's character matching so it retains the actual platform typeface,
                    // including fonts that cannot subsequently be resolved by their family name.
                    if (!fontManager.SystemFonts.TryMatchCharacter(character, FontStyle.Normal, weight,
                            FontStretch.Normal, null, FontCulture, out var typeface) ||
                        !fontManager.TryGetGlyphTypeface(typeface, out var glyphTypeface) ||
                        !glyphTypeface.CharacterToGlyphMap.TryGetGlyph(character, out _) ||
                        !families.Add(typeface.FontFamily.Name))
                    {
                        continue;
                    }

                    Fallbacks.Add(new FontFallback { FontFamily = typeface.FontFamily });
                }
            }

            if (families.Count == 0)
            {
                global::Android.Util.Log.Warn(nameof(AndroidFontHelper), "系统字符匹配未找到可用的字体回退。");
            }
            else
            {
                global::Android.Util.Log.Info(nameof(AndroidFontHelper),
                    $"已缓存系统中文字体回退：{string.Join(", ", families)}");
            }
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Warn(nameof(AndroidFontHelper), exception.ToString());
        }

        return true;
    }
}
