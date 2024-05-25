using ClassIsland.Core.Protobuf.Enum;

using Google.Protobuf;

namespace ClassIsland.Core.Models.Management;

public class ClientCommandEventArgs : EventArgs
{
    public CommandTypes Type { get; set; }

    public ByteString Payload { get; set; } = ByteString.Empty;
}