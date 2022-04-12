using FrostEvo.Core.Networking.Transmission;

namespace FrostEvo.Core.Networking.Connectivity;

public class NetworkEvents
{
    public delegate Task ClientConnection(NetworkActor networkActor);
    public delegate Task ClientReceive(NetworkActor networkActor, NetMsg netMsg);
}