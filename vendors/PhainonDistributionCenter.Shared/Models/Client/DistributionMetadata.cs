using System;
using System.Collections.Generic;

namespace PhainonDistributionCenter.Shared.Models.Client;

/// <summary>
/// 代表分发系统的基本信息。
/// </summary>
public class DistributionMetadata
{
    /// <summary>
    /// 代表分发系统中启用的通道
    /// </summary>
    public Dictionary<Guid, DistributionChannel> Channels { get; set; } = [];

    /// <summary>
    /// 默认通道 ID
    /// </summary>
    public Guid DefaultChannelId { get; set; } = Guid.Empty;

    /// <summary>
    /// 代表一个分发通道。
    /// </summary>
    public class DistributionChannel
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = "";
    }
}