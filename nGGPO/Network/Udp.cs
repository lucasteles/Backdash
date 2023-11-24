using nGGPO.Serialization;

namespace nGGPO.Network;

class Udp(int bindingPort) : UdpPeerClient<UdpMsg>(bindingPort, new UdpMsgBinarySerializer());