using nGGPO.Serialization;

namespace nGGPO.Network;

sealed class Udp(int bindingPort) : UdpPeerClient<UdpMsg>(bindingPort, new UdpMsgBinarySerializer());
