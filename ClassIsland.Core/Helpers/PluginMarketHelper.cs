using System.IO;
using System.Text.Json;
using ClassIsland.Core.Models.Plugin;
using Octokit;
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
    /// <param name="token">GitHub 个人访问令牌</param>
    public static async Task GeneratePluginIndexFromManifests(string input, string output, string? indexBase=null, string? token=null)
    {
        try
        {

            var manifests = Directory.EnumerateFiles(input).Where(x => Path.GetExtension(x) == ".yml");
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var index = string.IsNullOrWhiteSpace(indexBase)
                ? new PluginIndex()
                : JsonSerializer.Deserialize<PluginIndex>(await File.ReadAllTextAsync(indexBase))
                  ?? new PluginIndex();
            var github = new GitHubClient(new ProductHeaderValue("ClassIsland.PluginIndexResolver"));
            if (!string.IsNullOrEmpty(token))
            {
                github.Credentials = new Credentials(token);
            }
            const string root = "https://github.com";
            foreach (var mfPath in manifests)
            {
                var mfText = await File.ReadAllTextAsync(mfPath);
                var manifest = deserializer.Deserialize<PluginRepoManifest>(mfText);

                var repo = await github.Repository.Get(manifest.RepoOwner, manifest.RepoName);
                if (repo == null)
                {
                    await Console.Error.WriteLineAsync(
                        $"error: 插件 {manifest.Id} 的仓库路径 {manifest.RepoOwner}/{manifest.RepoName} 无效，已跳过。");
                    continue;
                }

                var latest = (await github.Repository.Release.GetAll(repo.Id))
                    .Where(x => Version.TryParse(x.TagName, out _)).MaxBy(x => Version.Parse(x.TagName));
                if (latest == null)
                {
                    await Console.Error.WriteLineAsync(
                        $"error: 插件 {manifest.Id} 的仓库路径 {manifest.RepoOwner}/{manifest.RepoName} 中没有标记为最新的发行版，已跳过。");
                    continue;
                }

                var asset = latest.Assets.FirstOrDefault(x => x.Name.EndsWith(".cipx"));
                if (asset == null)
                {
                    await Console.Error.WriteLineAsync(
                        $"error: 插件 {manifest.Id} 的仓库路径 {manifest.RepoOwner}/{manifest.RepoName} 中最新的发行版中没有有效的插件包资产，已跳过。");
                    continue;
                }

                var md5 = ChecksumHelper.ExtractHashInfo(latest.Body, asset.Name);
                manifest.Version = latest.TagName;
                manifest.Readme =
                    $"{{root}}/{manifest.RepoOwner}/{manifest.RepoName}/raw/{manifest.AssetsRoot}/{manifest.Readme}";
                index.Plugins.Add(new PluginIndexItem()
                {
                    Manifest = manifest,
                    DownloadMd5 = md5,
                    DownloadUrl = asset.BrowserDownloadUrl.Replace(root, "{root}"),
                    RealIconPath = $"{{root}}/{manifest.RepoOwner}/{manifest.RepoName}/raw/{manifest.AssetsRoot}/{manifest.Icon}"
                });
                Console.WriteLine($"成功添加插件 {manifest.Id}");
            }

            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(index));
            Console.Write("OK!");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }
    }
}