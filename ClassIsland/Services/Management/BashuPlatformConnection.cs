using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Shared.Abstraction.Services;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Management;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.Management;

/// <summary>
/// 两江巴蜀智慧教研平台集控连接器
/// 负责实现设备配对 (POST /api/display-client/pair)、
/// 轮询课表与通知广播 (GET /api/display-client/poll)、
/// 通知确认 (POST /api/display-client/ack) 等核心通信
/// </summary>
public class BashuPlatformConnection : IManagementServerConnection
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private ILogger<BashuPlatformConnection> Logger { get; } = App.GetService<ILogger<BashuPlatformConnection>>();
    public ManagementSettings Settings { get; }
    public Guid ClientGuid { get; }

    private HttpClient HttpClient { get; } = new();

    public event EventHandler<ClientCommandEventArgs>? CommandReceived;

    public BashuPlatformConnection(ManagementSettings settings, Guid clientGuid)
    {
        Settings = settings;
        ClientGuid = clientGuid;
        ConfigureHttpClient();
        Logger.LogInformation("初始化两江巴蜀智慧教研平台连接器，服务器地址：{}", Settings.BashuServerUrl);
    }

    private void ConfigureHttpClient()
    {
        HttpClient.BaseAddress = new Uri(string.IsNullOrWhiteSpace(Settings.BashuServerUrl) ? "https://bashu.cqaibase.cn" : Settings.BashuServerUrl.TrimEnd('/'));
        HttpClient.DefaultRequestHeaders.Accept.Clear();
        HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(Settings.BashuDeviceToken))
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Settings.BashuDeviceToken);
        }
    }

    public void UpdateToken(string token)
    {
        Settings.BashuDeviceToken = token;
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// 使用 6 位大屏配对码进行绑定
    /// </summary>
    public async Task<BashuPairResult> PairAsync(string pairingCode, string deviceName)
    {
        try
        {
            var reqBody = JsonSerializer.Serialize(new
            {
                code = pairingCode.Trim(),
                deviceName = string.IsNullOrWhiteSpace(deviceName) ? "班级多媒体大屏" : deviceName.Trim(),
                platform = "windows"
            });
            var content = new StringContent(reqBody, Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync("/api/display-client/pair", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning("两江巴蜀平台设备配对失败：{} - {}", response.StatusCode, responseJson);
                return new BashuPairResult { Success = false, ErrorMessage = "配对失败，请检查配对码是否正确或已过期" };
            }

            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            var token = root.GetProperty("token").GetString() ?? "";
            var device = root.GetProperty("device");
            var className = device.TryGetProperty("className", out var cn) ? cn.GetString() ?? "" : "";
            var classId = device.TryGetProperty("classId", out var cid) ? cid.GetInt32() : 0;
            var deviceId = device.TryGetProperty("id", out var did) ? did.GetInt32() : 0;

            UpdateToken(token);
            Settings.BashuClassName = className;
            Settings.BashuDeviceName = deviceName;
            Settings.ClassIdentity = className;

            Logger.LogInformation("两江巴蜀平台配对成功！班级：{} (ID: {})，设备ID: {}", className, classId, deviceId);
            return new BashuPairResult
            {
                Success = true,
                Token = token,
                ClassName = className,
                ClassId = classId,
                DeviceId = deviceId
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "配对过程发生异常");
            return new BashuPairResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// 轮询课表、天气与通知广播
    /// </summary>
    public async Task<string?> PollAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Settings.BashuDeviceToken))
        {
            return null;
        }

        try
        {
            var response = await HttpClient.GetAsync("/api/display-client/poll", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Logger.LogWarning("设备 Token 已失效或被解绑");
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogDebug("轮询平台数据返回状态码：{}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogDebug("轮询平台数据失败：{}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 确认已接收通知
    /// </summary>
    public async Task<bool> AcknowledgeNotificationAsync(long notificationId)
    {
        if (string.IsNullOrWhiteSpace(Settings.BashuDeviceToken) || notificationId <= 0)
        {
            return false;
        }

        try
        {
            var reqBody = JsonSerializer.Serialize(new { notificationId });
            var content = new StringContent(reqBody, Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync("/api/display-client/ack", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "向平台确认通知收到异常：{}", notificationId);
            return false;
        }
    }

    public Task<ManagementManifest> GetManifest()
    {
        return Task.FromResult(new ManagementManifest
        {
            OrganizationName = "两江巴蜀智慧教研平台",
            ServerKind = ManagementServerKind.BashuPlatform
        });
    }

    public Task<T> GetJsonAsync<T>(string url) where T : class
    {
        throw new NotSupportedException("两江巴蜀平台直接通过 PollAsync 进行数据同步。");
    }

    public Task<T> SaveJsonAsync<T>(string url, string path) where T : class
    {
        throw new NotSupportedException("两江巴蜀平台直接通过 PollAsync 进行数据同步。");
    }
}

public class BashuPairResult
{
    public bool Success { get; set; }
    public string Token { get; set; } = "";
    public string ClassName { get; set; } = "";
    public int ClassId { get; set; }
    public int DeviceId { get; set; }
    public string ErrorMessage { get; set; } = "";
}
