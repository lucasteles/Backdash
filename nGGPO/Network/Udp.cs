using System;
using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network;

class Udp(int bindingPort) : UdpPeerClient<UdpMsg>(bindingPort, new UdpMsgBinarySerializer())
{
    class UdpMsgBinarySerializer : IBinarySerializer<UdpMsg>
    {
        public UdpMsg Deserialize(in ReadOnlySpan<byte> data)
        {
            var offset = 0;
            NetworkBufferReader reader = new(data, ref offset);

            var msg = new UdpMsg();
            msg.Deserialize(reader);
            return msg;
        }

        public int Serialize(ref UdpMsg data, Span<byte> buffer)
        {
            var offset = 0;
            NetworkBufferWriter writer = new(buffer, ref offset);
            data.Serialize(writer);
            return offset;
        }
    }
    
    
}