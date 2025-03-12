using System.Diagnostics;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Client;
using Backdash.Network.Messages;

namespace Backdash.Network.Protocol.Comm;

interface IProtocolOutbox : IMessageSender, IMessageHandler<ProtocolMessage>;

sealed class ProtocolOutbox(
    ProtocolState state,
    IPeerClient<ProtocolMessage> peer,
    Logger logger
) : IProtocolOutbox
{
    int nextSendSeq;

    public bool SendMessage(in ProtocolMessage msg) => peer.TrySendTo(state.PeerAddress.Address, in msg, this);

    public void BeforeSendMessage(ref ProtocolMessage message)
    {
        message.Header.Magic = state.SyncNumber;
        message.Header.SequenceNumber = (ushort)nextSendSeq;
        nextSendSeq++;

        logger.Write(LogLevel.Trace, $"send {message} on {state.Player}");
    }

    public void AfterSendMessage(int bytesSent)
    {
        state.Stats.Send.LastTime = Stopwatch.GetTimestamp();
        state.Stats.Send.TotalBytes += (ByteSize)bytesSent;
        state.Stats.Send.TotalPackets++;
    }
}
