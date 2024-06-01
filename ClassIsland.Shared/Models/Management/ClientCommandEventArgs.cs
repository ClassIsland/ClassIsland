using ClassIsland.Shared.Protobuf.Enum;

using Google.Protobuf;

namespace ClassIsland.Shared.Models.Management;

/// <summary>
/// 集控命令事件参数
/// </summary>
public class ClientCommandEventArgs : EventArgs
{
    /// <summary>
    /// 命令类型
    /// </summary>
    public CommandTypes Type { get; set; }

    /// <summary>
    /// Grpc负载
    /// </summary>
    public ByteString Payload { get; set; } = ByteString.Empty;
}