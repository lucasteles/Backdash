using nGGPO.Network.Messages;

namespace nGGPO.Network.Protocol.Internal;

interface IProtocolLogger
{
    void LogMsg(in string log, in ProtocolMessage msg);
    void LogEvent(in string log, in ProtocolEventData evt);
}

sealed class ProtocolLogger(ILogger logger) : IProtocolLogger
{
    public void LogMsg(in string log, in ProtocolMessage msg) =>
        // LATER: check original source
        logger.Info($"{log}: {msg}");

    public void LogEvent(in string log, in ProtocolEventData evt) =>
        // LATER: check original source
        logger.Info($"{log}: {evt}");
}
