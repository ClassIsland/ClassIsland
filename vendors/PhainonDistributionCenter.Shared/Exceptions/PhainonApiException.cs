using System;
using PhainonDistributionCenter.Shared.Enums.Api;

namespace PhainonDistributionCenter.Shared.Exceptions;

/// <summary>
/// PhainonDistributionCenter api 异常
/// </summary>
public class PhainonApiException : Exception
{
    /// <summary>
    /// 错误状态码
    /// </summary>
    public StatusCodes Code { get; }

    internal PhainonApiException(StatusCodes code, string message) : base(message)
    {
        Code = code;
    }
}