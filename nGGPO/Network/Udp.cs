using System.Threading.Tasks;
using nGGPO.Serialization;

namespace nGGPO.Network;

class Udp : UdpPeerClient<UdpMsg>, IPollLoopSink
{
    public Udp(int bindingPort) : base(bindingPort, new StructMarshalBinarySerializer<UdpMsg>())
    {
    }

    public Task<bool> OnLoopPoll(object? cookie)
    {
        // TODO: precisa?
        return Task.FromResult(true);
    }
}