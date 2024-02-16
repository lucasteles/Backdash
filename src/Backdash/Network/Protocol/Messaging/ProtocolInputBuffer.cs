using System.Diagnostics;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Messages;
using Backdash.Sync;

namespace Backdash.Network.Protocol.Messaging;

interface IProtocolInputBuffer
{
    int PendingNumber { get; }
    GameInput LastSent { get; }
    AddInputResult SendInput(in GameInput input);
    void SendPendingInputs();
}

enum AddInputResult : byte
{
    Ok = 0,
    FullQueue,
    MessageBodyOverflow,
    AlreadyAcked,
    NotRunning,
}

sealed class ProtocolInputBuffer(
    ProtocolOptions options,
    ProtocolState state,
    Logger logger,
    ITimeSync timeSync,
    IMessageSender sender,
    IProtocolInbox inbox
) : IProtocolInputBuffer
{
    GameInput lastAckedInput = GameInput.Create(1, Frame.Null);

    readonly Queue<GameInput> pendingOutput = new(options.MaxInputQueue);
    readonly Memory<byte> workingBufferMemory = Mem.CreatePinnedBuffer(WorkingBufferSize);

    public GameInput LastSent { get; private set; } = GameInput.Create(1, Frame.Null);
    public int PendingNumber => pendingOutput.Count;

    static ProtocolInputBuffer() =>
        Trace.Assert(Max.InputSizeInBytes * ByteSize.ByteToBits < 1 << BitOffsetWriter.NibbleSize);

    bool IsQueueFull() => pendingOutput.Count >= options.MaxInputQueue;

    public AddInputResult SendInput(in GameInput input)
    {
        if (state.CurrentStatus is not ProtocolStatus.Running) return AddInputResult.NotRunning;
        if (IsQueueFull()) return AddInputResult.FullQueue;
        if (input.Frame < inbox.LastAckedFrame) return AddInputResult.AlreadyAcked;

        timeSync.AdvanceFrame(in input, in state.Fairness);

        pendingOutput.Enqueue(input);
        var createMessageResult = CreateInputMessage(out var inputMessage);
        sender.SendMessage(in inputMessage);

        if (createMessageResult is not AddInputResult.Ok)
            return createMessageResult;

        return AddInputResult.Ok;
    }

    public const int WorkingBufferSize = Max.TotalInputSizeInBytes;

    public void SendPendingInputs()
    {
        CreateInputMessage(out var inputMessage);
        sender.SendMessage(in inputMessage);
    }

    AddInputResult CreateInputMessage(out ProtocolMessage protocolMessage)
    {
        Span<byte> workingBuffer = workingBufferMemory.Span;
        Trace.Assert(workingBuffer.Length >= WorkingBufferSize);
        Span<byte> lastBytes = workingBuffer[..Max.TotalInputSizeInBytes];

        var lastAckFrame = inbox.LastAckedFrame;
        InputMessage inputMessage = new()
        {
            AckFrame = inbox.LastReceivedInput.Frame,
        };

        GameInput next;
        while (pendingOutput.TryPeek(out next) && next.Frame < lastAckFrame)
        {
            var acked = pendingOutput.Dequeue();
            logger.Write(LogLevel.Trace, $"Skipping past frame:{acked.Frame} current is {lastAckFrame}");
            lastAckedInput = acked;
        }

        lastAckedInput.CopyTo(lastBytes);

        if (pendingOutput.TryPeek(out next))
        {
            inputMessage.InputSize = (byte)next.Size;
            inputMessage.StartFrame = next.Frame;

            Trace.Assert(lastAckedInput.Frame.IsNull || lastAckedInput.Frame.Next() == next.Frame);

            if (lastAckedInput.Frame.IsNull && lastAckedInput.Size < next.Size)
                lastAckedInput.Size = next.Size;
        }
        var compressor = InputEncoder.GetCompressor(ref inputMessage, lastBytes);

        var messageBodyOverflow = false;
        var count = pendingOutput.Count;
        var n = 0;
        while (n++ < count)
        {
            next = pendingOutput.Dequeue();
            if (compressor.Write(next.Buffer))
            {
                LastSent = next;
                pendingOutput.Enqueue(next);
            }
            else
            {
                logger.Write(LogLevel.Warning,
                    $"Max input size reached. Sending inputs until frame {next.Frame.Previous()}");
                pendingOutput.EnqueueNext(in next);
                messageBodyOverflow = true;
                break;
            }
        }

        Trace.Assert(inputMessage.NumBits <= Max.CompressedBytes * ByteSize.ByteToBits);
        inputMessage.InputSize = (byte)LastSent.Size;
        inputMessage.NumBits = compressor.BitOffset;
        inputMessage.DisconnectRequested = state.CurrentStatus is ProtocolStatus.Disconnected;

        if (state.LocalConnectStatuses.AnyConnected())
            state.LocalConnectStatuses.CopyTo(inputMessage.PeerConnectStatus);

        protocolMessage = new(MsgType.Input)
        {
            Input = inputMessage,
        };

        return messageBodyOverflow
            ? AddInputResult.MessageBodyOverflow
            : AddInputResult.Ok;
    }
}
