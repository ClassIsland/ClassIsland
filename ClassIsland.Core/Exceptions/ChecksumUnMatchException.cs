namespace ClassIsland.Core.Exceptions;

/// <summary>
/// 校验和不匹配异常。
/// </summary>
public class ChecksumUnMatchException : Exception
{
    /// <inheritdoc />
    public ChecksumUnMatchException() 
    {
        
    }

    /// <inheritdoc />
    public ChecksumUnMatchException(string message) : base(message)
    {

    }
}
