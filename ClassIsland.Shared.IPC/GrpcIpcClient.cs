using Grpc.Core;
using GrpcDotNetNamedPipes;

namespace ClassIsland.Shared.IPC;

public class GrpcIpcClient
{
    public static string PipeName { get; } = "ClassIsland.IPC";

    public NamedPipeChannel Channel;

    public GrpcIpcClient() : this(".", PipeName)
    {
        
    }

    public GrpcIpcClient(string serverName, string pipeName)
    {
        Channel = new NamedPipeChannel(serverName, pipeName);
    }
}