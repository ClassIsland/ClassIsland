using PhainonDistributionCenter.Shared.Enums.Api;

namespace PhainonDistributionCenter.Shared.Models.Api.Responses;

/// <summary>
/// 代表请求的结果
/// </summary>
public class Result
{
    /// <summary>
    /// 代表请求的结果
    /// </summary>
    public Result(StatusCodes code, string message = "")
    {
        Code = code;
        Message = message;
    }

    /// <summary>
    /// 代表请求的结果
    /// </summary>
    public Result()
    {
        
    }

    /// <summary>
    /// 状态代码
    /// </summary>
    public StatusCodes Code { get; set; } = StatusCodes.Unknown;

    /// <summary>
    /// 状态信息
    /// </summary>
    public string Message { get; set; } = "";


}

/// <inheritdoc />
public class Result<T> : Result
{
    /// <inheritdoc />
    public Result(StatusCodes code, T? content, string message = "") : base(code, message)
    {
        Content = content;
    }
    
    /// <inheritdoc />
    public Result() : base()
    {
        
    }

    /// <summary>
    /// 返回的信息
    /// </summary>
    public T? Content { get; set; }
}