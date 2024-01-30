using nGGPO.Network.Messages;

namespace nGGPO.Network.Protocol.Internal;

interface IProtocolLogger
{
    void LogMsg(string send, in ProtocolMessage msg);
    void LogEvent(string queuingEvent, ProtocolEventData evt);
}

sealed class ProtocolLogger : IProtocolLogger
{
    public void LogMsg(string send, in ProtocolMessage msg)
    {
        // Implement later
    }

    public void LogEvent(string queuingEvent, ProtocolEventData evt)
    {
        // Implement later
    }
}
