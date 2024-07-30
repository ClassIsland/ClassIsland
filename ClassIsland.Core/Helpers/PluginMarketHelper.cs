using System.IO;
using System.Text.Json;
using ClassIsland.Core.Models.Plugin;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ClassIsland.Core.Helpers;

/// <summary>
/// 插件市场工具
/// </summary>
public class PluginMarketHelper
{
    /// <summary>
    /// 从插件清单生成插件仓库索引。
    /// </summary>
    /// <param name="input">插件清单目录</param>
    /// <param name="output">插件索引输出目录</param>
    /// <param name="indexBase">索引基础</param>
    public static void GeneratePluginIndexFromManifests(string input, string output, string? indexBase=null)
    {
        var manifests = Directory.EnumerateFiles(input).Where(x => Path.GetExtension(x) == ".yml");
        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var index = string.IsNullOrWhiteSpace(indexBase)
            ? new PluginIndex()
            : JsonSerializer.Deserialize<PluginIndex>(File.ReadAllText(indexBase))
            ?? new PluginIndex();
        foreach (var mfPath in manifests)
        {
            var mfText = File.ReadAllText(mfPath);
            var manifest = deserializer.Deserialize<PluginRepoManifest>(mfText);

            index.Plugins.Add(new PluginIndexItem()
            {
                Manifest = manifest
            });
        }

        File.WriteAllText(output, JsonSerializer.Serialize(index));
        Console.Write("👌");
    }
}