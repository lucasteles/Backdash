using Backdash.Network.Messages;
using Backdash.Tests.TestUtils;
using Backdash.Synchronizing.Input;
using Xunit.Abstractions;

namespace Backdash.Tests.Viewers;

[Trait("Type", "View")]
public class StructLayoutViewer(ITestOutputHelper output)
{
    [NonCIFact] public void GameInput() => output.PrintLayout<GameInput<ushort>>();
    [NonCIFact] public void GameTestInput() => output.PrintLayout<GameInput>();
    [NonCIFact] public void SynchronizedInput() => output.PrintLayout<SynchronizedInput<ushort>>();
    [NonCIFact] public void ConnectStatus() => output.PrintLayout<ConnectStatus>();

    [NonCIFact] public void PeerEventInfo() => output.PrintLayout<PeerEventInfo>();
    [NonCIFact] public void SynchronizingEventInfo() => output.PrintLayout<SynchronizingEventInfo>();
    [NonCIFact] public void ConnectionInterruptedEventInfo() => output.PrintLayout<ConnectionInterruptedEventInfo>();
    [NonCIFact] public void SynchronizedEventInfo() => output.PrintLayout<SynchronizedEventInfo>();

    [NonCIFact] public void Header() => output.PrintLayout<Header>();
    [NonCIFact] public void SyncRequest() => output.PrintLayout<SyncRequest>();
    [NonCIFact] public void SyncReply() => output.PrintLayout<SyncReply>();
    [NonCIFact] public void QualityReport() => output.PrintLayout<QualityReport>();
    [NonCIFact] public void QualityReply() => output.PrintLayout<QualityReply>();
    [NonCIFact] public void InputAck() => output.PrintLayout<InputAck>();
    [NonCIFact] public void KeepAlive() => output.PrintLayout<KeepAlive>();
    [NonCIFact] public void ConsistencyCheckRequest() => output.PrintLayout<ConsistencyCheckRequest>();
    [NonCIFact] public void ConsistencyCheckReply() => output.PrintLayout<ConsistencyCheckReply>();
    [NonCIFact] public void InputMessage() => output.PrintLayout<InputMessage>();
    [NonCIFact] public void InputMessageBuffer() => output.PrintLayout<InputMessageBuffer>();
    [NonCIFact] public void PeerStatusBuffer() => output.PrintLayout<PeerStatusBuffer>();

    [NonCIFact] public void ProtocolMessage() => output.PrintLayout<ProtocolMessage>();
}
