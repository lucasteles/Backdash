using Backdash.Network.Protocol;
namespace Backdash.Network;

interface IProtocolNetworkEventHandler : IDisposable
{
    void OnNetworkEvent(in ProtocolEventInfo evt);
    void OnNetworkEvent(in ProtocolEvent evt, in PlayerHandle player) =>
        OnNetworkEvent(new(evt, player));
}
