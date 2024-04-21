using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ClassIsland.Core.Helpers;

namespace ClassIsland.Helpers;

public static class WebRequestHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private static HttpClient HttpClient { get; } = new();

    public static async Task<T> GetJson<T>(Uri uri)
    {
        var str = await HttpClient.GetStringAsync(uri);
        var r = JsonSerializer.Deserialize<T>(str, JsonOptions);
        return r == null ? throw new Exception("Json.Deserialize returned null value.") : r;
    }

    public static async Task<T> SaveJson<T>(Uri uri, string path)
    {
        var j = await GetJson<T>(uri);
        ConfigureFileHelper.SaveConfig(path, j);
        return j;
    }
}