using ClassIsland.Shared.Protobuf.Enum;

using Google.Protobuf;

namespace ClassIsland.Shared.Models.Management;

public class ClientCommandEventArgs : EventArgs
{
    public CommandTypes Type { get; set; }

    public ByteString Payload { get; set; } = ByteString.Empty;
}