using nGGPO.Network.Messages;

namespace nGGPO.Network.Protocol.Internal;

sealed class ProtocolLogger
{
    public void LogMsg(string send, in ProtocolMessage msg)
    {
        throw new NotImplementedException();
    }

    public void LogEvent(string queuingEvent, ProtocolEventData evt)
    {
        throw new NotImplementedException();
    }
}
