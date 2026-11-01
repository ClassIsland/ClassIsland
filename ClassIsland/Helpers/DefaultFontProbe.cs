using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace ClassIsland.Helpers;

internal sealed record FontVariationAxis(string Tag, double Minimum, double Default, double Maximum);

internal sealed record DefaultFontProbeResult(
    string FamilyName, bool? IsVariable, IReadOnlyList<FontVariationAxis> Axes)
{
    internal FontVariationAxis? WeightAxis => Axes.FirstOrDefault(x => x.Tag == "wght");

    internal string DisplayText => $"实际字体：{FamilyName}\n可变字体：{IsVariable switch
    {
        true => "是",
        false => "否",
        null => "无法判断"
    }}\n" + (IsVariable == true
        ? string.Join("\n", Axes.Select(x => $"{x.Tag}：{x.Minimum:0.##}–{x.Maximum:0.##}（默认 {x.Default:0.##}）")) +
          (WeightAxis == null ? "\n无 wght 字重轴" : "")
        : IsVariable == null ? "字体数据暂不可读，请稍后重新打开或刷新。" : "无可变字重轴");
}

internal static class DefaultFontProbe
{
    internal static DefaultFontProbeResult Read()
    {
        var familyName = "未解析";
        try
        {
            if (!FontManager.Current.TryGetGlyphTypeface(new Typeface(FontFamily.Default), out var typeface))
            {
                return new DefaultFontProbeResult(familyName, null, []);
            }

            familyName = typeface.FamilyName;
            // The font manager owns this typeface; only inspect its tables.
            var font = typeface.PlatformTypeface;
            if (!font.TryGetTable(new OpenTypeTag('f', 'v', 'a', 'r'), out var table))
            {
                var readable = font.TryGetTable(new OpenTypeTag('h', 'e', 'a', 'd'), out var head) &&
                               head.Length >= 54;
                return new DefaultFontProbeResult(familyName, readable ? false : null, []);
            }

            return TryReadAxes(table.Span, out var axes)
                ? new DefaultFontProbeResult(familyName, true, axes)
                : new DefaultFontProbeResult(familyName, null, []);
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            return new DefaultFontProbeResult(familyName, null, []);
        }
    }

    internal static bool TryReadAxes(ReadOnlySpan<byte> table, out IReadOnlyList<FontVariationAxis> axes)
    {
        axes = [];
        if (table.Length < 16 || BinaryPrimitives.ReadUInt32BigEndian(table) != 0x00010000 ||
            BinaryPrimitives.ReadUInt16BigEndian(table[6..]) != 2)
        {
            return false;
        }

        var offset = BinaryPrimitives.ReadUInt16BigEndian(table[4..]);
        var count = BinaryPrimitives.ReadUInt16BigEndian(table[8..]);
        var size = BinaryPrimitives.ReadUInt16BigEndian(table[10..]);
        var instanceCount = BinaryPrimitives.ReadUInt16BigEndian(table[12..]);
        var instanceSize = BinaryPrimitives.ReadUInt16BigEndian(table[14..]);
        var axesEnd = offset + (long)count * size;
        if (offset < 16 || count == 0 || size < 20 || axesEnd > table.Length ||
            (instanceCount > 0 && (instanceSize < 4 + count * 4 ||
                                   axesEnd + (long)instanceCount * instanceSize > table.Length)))
        {
            return false;
        }

        List<FontVariationAxis> result = [];
        HashSet<string> tags = [];
        for (var i = 0; i < count; i++)
        {
            var record = table.Slice(offset + i * size, size);
            var tag = new OpenTypeTag(BinaryPrimitives.ReadUInt32BigEndian(record)).ToString();
            var minimum = BinaryPrimitives.ReadInt32BigEndian(record[4..]) / 65536.0;
            var defaultValue = BinaryPrimitives.ReadInt32BigEndian(record[8..]) / 65536.0;
            var maximum = BinaryPrimitives.ReadInt32BigEndian(record[12..]) / 65536.0;
            if (!tags.Add(tag) || minimum > defaultValue || defaultValue > maximum)
            {
                return false;
            }

            result.Add(new FontVariationAxis(tag, minimum, defaultValue, maximum));
        }

        axes = result;
        return true;
    }
}
