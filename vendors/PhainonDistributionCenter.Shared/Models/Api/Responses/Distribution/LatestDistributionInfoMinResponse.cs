using System;

namespace PhainonDistributionCenter.Shared.Models.Api.Responses.Distribution;

/// <summary>
/// 代表获取最新分发信息的小型响应
/// </summary>
public class LatestDistributionInfoMinResponse
{
    /// <summary>
    /// 对应版本的 Id
    /// </summary>
    public Guid DistributionId { get; set; } = Guid.Empty;

    /// <summary>
    /// 对应的版本
    /// </summary>
    public string Version { get; set; } = "0.0.0.0";
}