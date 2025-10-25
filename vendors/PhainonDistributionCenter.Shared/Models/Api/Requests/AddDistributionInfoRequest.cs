using System.Collections.Generic;

namespace PhainonDistributionCenter.Shared.Models.Api.Requests;

/// <summary>
/// 代表添加发布信息的 api 请求体。
/// </summary>
public class AddDistributionInfoRequest
{
    /// <summary>
    /// 发行日志
    /// </summary>
    public string ChangeLog { get; set; } = null!;

    /// <summary>
    /// 当前发行版包含的子频道
    /// </summary>
    public List<DistributionSubChannel> SubChannels { get; set; } = [];

    /// <summary>
    /// 代表请求体中的子频道信息
    /// </summary>
    public class DistributionSubChannel
    {
        /// <summary>
        /// 子频道的目标 OS，如 windows
        /// </summary>
        public string Os { get; set; } = "unknown";
    
        /// <summary>
        /// 子频道的目标 CPU 架构，如 x64
        /// </summary>
        public string Arch { get; set; } = "unknown";
    
        /// <summary>
        /// 子频道的目标打包方式，如 folder（文件夹打包）
        /// </summary>
        public string Package { get; set; } = "unknown";
    
        /// <summary>
        /// 子频道的目标构建方式，如 full（完整构建）
        /// </summary>
        public string BuildType { get; set; } = "unknown";

        /// <summary>
        /// 当前子频道的文件图
        /// </summary>
        public string FileMap { get; set; } = null!;

        /// <summary>
        /// 下载归档文件名，仅用于打包流程
        /// </summary>
        public string ArchiveName { get; set; } = "";

        /// <summary>
        /// 当前子频道的文件图签名
        /// </summary>
        public string FileMapSignature { get; set; } = null!;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Os}_{Arch}_{BuildType}_{Package}";
        }
    }
}