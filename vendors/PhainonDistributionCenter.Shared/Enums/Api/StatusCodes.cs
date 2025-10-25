namespace PhainonDistributionCenter.Shared.Enums.Api;

/// <summary>
/// Api 接口的状态代码
/// </summary>
public enum StatusCodes
{
    /// <summary>
    /// 未知错误
    /// </summary>
    Unknown = -1,
    /// <summary>
    /// 操作成功完成
    /// </summary>
    Success = 0,

    #region GpgSignatureService

    /// <summary>
    /// 无法验证 GPG 签名
    /// </summary>
    GpgSignatureVerifyFailed = 1001,

    #endregion

    #region DistributionInfosController

    /// <summary>
    /// 无效的版本信息
    /// </summary>
    AddDistributionInvalidVersion = 2001,
    /// <summary>
    /// 找不到请求的大版本
    /// </summary>
    AddDistributionPrimaryVersionNotFound = 2002,

    #endregion

    #region DistributionsController

    /// <summary>
    /// 对应的通道上没有符合要求的发行版信息
    /// </summary>
    NoDistributionsAvailable = 3001,
    
    /// <summary>
    /// 找不到符合要求的发行版信息
    /// </summary>
    DistributionNotFound = 3002,

    #endregion

}