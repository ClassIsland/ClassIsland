using System.Text.Json;
using Avalonia.Platform;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Tutorial;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 注册教程组的 <see cref="IServiceCollection"/> 扩展。
/// </summary>
public static class TutorialRegistryExtensions
{
    /// <summary>
    /// 从 Avalonia 资源 Uri 注册一个教程组。
    /// </summary>
    public static IServiceCollection AddTutorialGroupByUri(this IServiceCollection services, Uri uri)
    {
        using var sr = new StreamReader(AssetLoader.Open(uri));
        var content = JsonSerializer.Deserialize<TutorialGroup>(sr.ReadToEnd());
        if (content == null)
        {
            throw new InvalidOperationException("无效的教程文件");
        }
        if (string.IsNullOrWhiteSpace(content.Id))
        {
            throw new InvalidOperationException("教程组 ID 无效");
        }
        if (ITutorialService.RegisteredTutorialGroups.Any(x => x.Id == content.Id))
        {
            throw new InvalidOperationException($"已注册具有相同 ID 的教学组 {content.Id}");
        }
        ITutorialService.RegisteredTutorialGroups.Add(content);
        return services;
    }
}