using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network.Messages;
using nGGPO.Utils;

namespace nGGPO.Network.Protocol.Gear;

sealed class InputProcessor
{
    readonly TimeSync timeSync;
    readonly InputCompressor inputCompressor;
    readonly ConnectStatus[] localConnectStatus;
    readonly MessageOutbox outbox;

    readonly CircularBuffer<GameInput> pendingOutput;
    GameInput lastSentInput;

    public InputProcessor(
        TimeSync timeSync,
        InputCompressor inputCompressor,
        ConnectStatus[] localConnectStatus,
        MessageOutbox outbox
    )
    {
        this.timeSync = timeSync;
        this.inputCompressor = inputCompressor;
        this.localConnectStatus = localConnectStatus;
        this.outbox = outbox;
        lastSentInput = GameInput.Empty;
        pendingOutput = new();
    }

    public CircularBuffer<GameInput> Pending => pendingOutput;

    public ValueTask SendInput(in GameInput input,
        ProtocolStatus currentProtocolState,
        GameInput lastReceivedInput,
        GameInput lastAckedInput,
        int localFrameAdvantage,
        int remoteFrameAdvantage,
        CancellationToken ct)
    {
        if (currentProtocolState is ProtocolStatus.Running)
        {
            /*
             * Check to see if this is a good time to adjust for the rift...
             */
            timeSync.AdvanceFrame(in input, localFrameAdvantage, remoteFrameAdvantage);

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
            Input = CreateInputMsg(currentProtocolState, lastReceivedInput, lastAckedInput),
        };

        return outbox.SendMsg(ref msg, ct);
    }

    InputMsg CreateInputMsg(
        ProtocolStatus currentProtocolState,
        GameInput lastReceivedInput,
        GameInput lastAckedInput
    )
    {
        if (pendingOutput.IsEmpty)
            return new();

        var compressedInput = inputCompressor.Compress(
            ref lastAckedInput,
            in pendingOutput,
            ref lastSentInput
        );

        compressedInput.AckFrame = lastReceivedInput.Frame;
        compressedInput.DisconnectRequested = currentProtocolState is not ProtocolStatus.Disconnected;

        if (localConnectStatus.Length > 0)
            localConnectStatus.CopyTo(compressedInput.PeerConnectStatus);

        return compressedInput;
    }
}
