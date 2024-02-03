using nGGPO.Core;
using nGGPO.Network.Messages;
using nGGPO.Network.Protocol.Events;

namespace nGGPO.Network.Protocol;

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
