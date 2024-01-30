using System.Threading.Channels;
using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network.Messages;
using nGGPO.Utils;

namespace nGGPO.Network.Protocol.Internal;

sealed class ProtocolInputQueue(
    TimeSync timeSync,
    Connections localConnections,
    IMessageSender sender,
    ProtocolInbox inbox,
    ProtocolState state
)
{
    GameInput lastSentInput = GameInput.Empty;
    GameInput lastAckedInput = GameInput.Empty;

    readonly CircularBuffer<GameInput> pendingOutput = new();

    readonly Channel<GameInput> inputQueue =
        Channel.CreateBounded<GameInput>(
            new BoundedChannelOptions(Max.InputQueue)
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = true,
                FullMode = BoundedChannelFullMode.Wait,
            });

    int pendingNumber;

    public int PendingNumber => pendingNumber;


    public async ValueTask SendInput(
        GameInput input,
        CancellationToken ct
    )
    {
        Tracer.Assert(
            Max.InputBytes * Max.MsgPlayers * Mem.ByteSize
            <
            1 << BitVector.BitOffset.NibbleSize
        );


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
            Interlocked.Increment(ref pendingNumber);
            await inputQueue.Writer.WriteAsync(input, ct).ConfigureAwait(false);
        }
    }

    public async Task StartPumping(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await inputQueue.Reader.WaitToReadAsync(ct).ConfigureAwait(false);

            ProtocolMessage msg = new(MsgType.Input)
            {
                Input = CreateInputMsg(inputQueue.Reader),
            };

            await sender.SendMessage(ref msg, ct).ConfigureAwait(false);
        }
    }

    InputMsg CreateInputMsg(ChannelReader<GameInput> reader)
    {
        if (pendingOutput.IsEmpty)
            return InputMsg.Empty;

        // inbox.LastAckedFrame,
        var compressedInput = InputEncoder.Compress(
            reader,
            lastAckedInput,
            ref lastSentInput,
            out int count
        );

        Interlocked.Add(ref pendingNumber, count);
        compressedInput.AckFrame = inbox.LastReceivedInput.Frame;
        compressedInput.DisconnectRequested = state.Status is not ProtocolStatus.Disconnected;

        if (localConnections.Length > 0)
            localConnections.Statuses.CopyTo(compressedInput.PeerConnectStatus);

        return compressedInput;
    }
}
