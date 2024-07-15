using Grpc.Core;
using GrpcDotNetNamedPipes;

namespace ClassIsland.Shared.IPC;

public class IpcClient
{
    public static string PipeName { get; } = "ClassIsland.IPC";

    public NamedPipeChannel Channel;

    public IpcClient() : this(".", PipeName)
    {
        
    }

    public IpcClient(string serverName, string pipeName)
    {
        Channel = new NamedPipeChannel(serverName, pipeName);
    }
}