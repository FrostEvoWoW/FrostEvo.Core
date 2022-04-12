using FrostEvo.Core.Networking.Transmission;

namespace FrostEvo.Core.Networking.Connectivity;

public interface INetworkActor
{
    public Task<bool> Handshake();
    public Task Send(byte[] msg);
    public Task Send(NetMsg msg);
    public Task Disconnect();
}