using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network.Messages;
using nGGPO.Utils;

namespace nGGPO.Network.Protocol;

sealed class InputProcessor
{
    readonly TimeSync timeSync;
    readonly InputCompressor inputCompressor;
    readonly ConnectStatus[] localConnectStatus;
    readonly ProtocolOutbox outbox;

    readonly CircularBuffer<GameInput> pendingOutput;
    GameInput lastReceivedInput;
    GameInput lastSentInput;
    GameInput lastAckedInput;

    public InputProcessor(
        TimeSync timeSync,
        InputCompressor inputCompressor,
        ConnectStatus[] localConnectStatus,
        ProtocolOutbox outbox
    )
    {
        this.timeSync = timeSync;
        this.inputCompressor = inputCompressor;
        this.localConnectStatus = localConnectStatus;
        this.outbox = outbox;
        lastReceivedInput = GameInput.Empty;
        lastSentInput = GameInput.Empty;
        lastAckedInput = GameInput.Empty;
        pendingOutput = new();
    }


    public ValueTask SendInput(in GameInput input,
        ProtocolState currentProtocolState,
        int localFrameAdvantage,
        int remoteFrameAdvantage,
        CancellationToken ct)
    {
        if (currentProtocolState is ProtocolState.Running)
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

        return SendPendingOutput(currentProtocolState, ct);
    }

    ValueTask SendPendingOutput(ProtocolState currentProtocolState, CancellationToken ct)
    {
        Tracer.Assert(
            Max.InputBytes * Max.MsgPlayers * Mem.ByteSize
            <
            1 << BitVector.BitOffset.NibbleSize
        );

        var input = CreateInputMsg(currentProtocolState);

        ProtocolMessage msg = new(MsgType.Input)
        {
            Input = input,
        };

        return outbox.SendMsg(ref msg, ct);
    }

    InputMsg CreateInputMsg(ProtocolState currentProtocolState)
    {
        if (pendingOutput.IsEmpty)
            return new();

        var compressedInput = inputCompressor.WriteCompressed(
            ref lastAckedInput,
            in pendingOutput,
            ref lastSentInput
        );

        compressedInput.AckFrame = lastReceivedInput.Frame;
        compressedInput.DisconnectRequested = currentProtocolState is not ProtocolState.Disconnected;

        if (localConnectStatus.Length > 0)
            localConnectStatus.CopyTo(compressedInput.PeerConnectStatus);

        return compressedInput;
    }
}
