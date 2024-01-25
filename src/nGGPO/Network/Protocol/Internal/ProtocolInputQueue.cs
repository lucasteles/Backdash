using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network.Messages;
using nGGPO.Utils;

namespace nGGPO.Network.Protocol.Internal;

sealed class ProtocolInputQueue(
    TimeSync timeSync,
    InputCompressor inputCompressor,
    Connections localConnections,
    IMessageSender sender
)
{
    GameInput lastSentInput = GameInput.Empty;

    readonly CircularBuffer<GameInput> pendingOutput = new();
    public CircularBuffer<GameInput> Pending => pendingOutput;

    public ValueTask SendInput(
        in GameInput input,
        ProtocolState state,
        GameInput lastReceived,
        GameInput lastAcked,
        CancellationToken ct
    )
    {
        if (state.Status is ProtocolStatus.Running)
        {
            /*
             * Check to see if this is a good time to adjust for the rift...
             */
            timeSync.AdvanceFrame(in input, state.Fairness);

            /*
             * Save this input packet
             *
             * XXX: This queue may fill up for spectators who do not ack input packets in a timely
             * manner.  When this happens, we can either resize the queue (ug) or disconnect them
             * (better, but still ug).  For the meantime, make this queue really big to decrease
             * the odds of this happening...
             */
            pendingOutput.Push(in input);
        }

        Tracer.Assert(
            Max.InputBytes * Max.MsgPlayers * Mem.ByteSize
            <
            1 << BitVector.BitOffset.NibbleSize
        );

        ProtocolMessage msg = new(MsgType.Input)
        {
            Input = CreateInputMsg(state.Status, lastReceived, lastAcked),
        };

        return sender.SendMessage(ref msg, ct);
    }

    InputMsg CreateInputMsg(
        ProtocolStatus protocolStatus,
        GameInput lastReceivedInput,
        GameInput lastAckedInput
    )
    {
        if (pendingOutput.IsEmpty)
            return InputMsg.Empty;

        var compressedInput = inputCompressor.Compress(
            in lastAckedInput,
            in pendingOutput,
            ref lastSentInput
        );

        compressedInput.AckFrame = lastReceivedInput.Frame;
        compressedInput.DisconnectRequested = protocolStatus is not ProtocolStatus.Disconnected;

        if (localConnections.Length > 0)
            localConnections.Statuses.CopyTo(compressedInput.PeerConnectStatus);

        return compressedInput;
    }
}
