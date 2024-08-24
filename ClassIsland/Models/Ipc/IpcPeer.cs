using CommunityToolkit.Mvvm.ComponentModel;
using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using dotnetCampus.Ipc.Pipes;

namespace ClassIsland.Models.Ipc;

public class IpcPeer(PeerProxy peerProxy, JsonIpcDirectRoutedClientProxy jsonPeerProxy) : ObservableRecipient
{

    public PeerProxy PeerProxy { get; } = peerProxy;

    public JsonIpcDirectRoutedClientProxy JsonPeerProxy { get; } = jsonPeerProxy;
}