using System.Runtime.CompilerServices;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Messages;
using Backdash.Options;
using Backdash.Serialization;
using Backdash.Synchronizing;
using Backdash.Synchronizing.Input;

namespace Backdash.Network.Protocol.Comm;

interface IProtocolInputBuffer<TInput> where TInput : unmanaged
{
    int PendingNumber { get; }
    GameInput<TInput> LastSent { get; }
    SendInputResult SendInput(in GameInput<TInput> input);
    SendInputResult SendPendingInputs();
}

enum SendInputResult : byte
{
    Ok = 0,
    FullQueue,
    MessageBodyOverflow,
    AlreadyAcked,
}

sealed class ProtocolInputBuffer<TInput> : IProtocolInputBuffer<TInput>
    where TInput : unmanaged
{
    readonly Queue<GameInput<TInput>> pendingOutput;
    readonly byte[] workingBufferMemory;
    int lastSentSize;
    int lastAckSize;
    GameInput<TInput> lastAckedInput = new();
    public GameInput<TInput> LastSent { get; private set; } = new();
    readonly int inputSize;
    readonly ProtocolOptions options;
    readonly IBinarySerializer<TInput> inputSerializer;
    readonly ProtocolState state;
    readonly Logger logger;
    readonly ITimeSync<TInput> timeSync;
    readonly IMessageSender sender;
    readonly IProtocolInbox<TInput> inbox;
    public int PendingNumber => pendingOutput.Count;

    public ProtocolInputBuffer(ProtocolOptions options,
        IBinarySerializer<TInput> inputSerializer,
        ProtocolState state,
        Logger logger,
        ITimeSync<TInput> timeSync,
        IMessageSender sender,
        IProtocolInbox<TInput> inbox)
    {
        this.options = options;
        this.inputSerializer = inputSerializer;
        this.state = state;
        this.logger = logger;
        this.timeSync = timeSync;
        this.sender = sender;
        this.inbox = inbox;
        inputSize = Unsafe.SizeOf<TInput>();
        ThrowIf.Assert(inputSize * ByteSize.ByteToBits < 1 << ByteSize.ByteToBits);
        workingBufferMemory = Mem.AllocatePinnedArray(WorkingBufferFactor * inputSize);
        pendingOutput = new(options.MaxPendingInputs);
    }

    const int WorkingBufferFactor = 3;
    bool IsQueueFull() => pendingOutput.Count >= options.MaxPendingInputs;

    public SendInputResult SendInput(in GameInput<TInput> input)
    {
        if (state.CurrentStatus is ProtocolStatus.Running)
        {
            if (IsQueueFull()) return SendInputResult.FullQueue;
            if (input.Frame < inbox.LastAckedFrame) return SendInputResult.AlreadyAcked;
            timeSync.AdvanceFrame(in input, in state.Fairness);
            pendingOutput.Enqueue(input);
        }

        return SendPendingInputs();
    }

    public SendInputResult SendPendingInputs()
    {
        ProtocolMessage message = new(MessageType.Input);
        var createMessageResult = FillInputMessage(ref message.Input);
        sender.SendMessage(in message);
        return createMessageResult;
    }

    SendInputResult FillInputMessage(ref InputMessage inputMessage)
    {
        var workingBuffer = workingBufferMemory.AsSpan();
        ThrowIf.Assert(workingBuffer.Length >= WorkingBufferFactor);
        var lastAckBytes = workingBuffer[..inputSize];
        var sendBuffer = workingBuffer.Slice(inputSize, inputSize);
        var currentBytes = workingBuffer.Slice(inputSize * 2, inputSize);

        var messageBodyOverflow = false;
        var lastAckFrame = inbox.LastAckedFrame;

        inputMessage.StartFrame = Frame.Zero;

        if (pendingOutput.Count > 0)
        {
            while (pendingOutput.Peek().Frame.Number < lastAckFrame.Number)
            {
                var acked = pendingOutput.Dequeue();
                logger.Write(LogLevel.Trace, $"Skipping past frame:{acked.Frame} current is {lastAckFrame}");
                lastAckedInput = acked;
                lastAckSize = inputSerializer.Serialize(in lastAckedInput.Data, lastAckBytes);
            }

            ThrowIf.Assert(lastAckedInput.Frame.IsNull || lastAckedInput.Frame.Next() == lastAckFrame);
            var current = pendingOutput.Peek();
            var currentSize = inputSerializer.Serialize(in current.Data, currentBytes);
            inputMessage.InputSize = (byte)currentSize;
            inputMessage.StartFrame = current.Frame;
            ThrowIf.Assert(lastAckedInput.Frame.IsNull || lastAckedInput.Frame.Next() == inputMessage.StartFrame);
            if (lastAckedInput.Frame.IsNull && lastSentSize < currentSize)
                lastSentSize = currentSize;
            if (lastAckSize is 0)
                sendBuffer.Clear();
            else
                lastAckBytes[..lastAckSize].CopyTo(sendBuffer);
            var compressor = InputEncoder.GetCompressor(ref inputMessage, sendBuffer);
            var count = pendingOutput.Count;
            var n = 0;
            while (n++ < count)
            {
                current = pendingOutput.Dequeue();
                if (n > 1)
                    currentSize = inputSerializer.Serialize(in current.Data, currentBytes);
                if (compressor.Write(currentBytes[..currentSize]))
                {
                    LastSent = current;
                    lastSentSize = currentSize;
                    pendingOutput.Enqueue(current);
                }
                else
                {
                    logger.Write(LogLevel.Warning,
                        $"Max input size reached. Sending inputs until frame {current.Frame.Previous()}");
                    pendingOutput.EnqueueNext(in current);
                    messageBodyOverflow = true;
                    break;
                }
            }

            inputMessage.InputSize = (byte)lastSentSize;
            inputMessage.NumBits = compressor.BitOffset;
            inputMessage.DisconnectRequested = state.CurrentStatus is ProtocolStatus.Disconnected;
        }

        inputMessage.AckFrame = inbox.LastReceivedInput.Frame;
        inputMessage.PeerCount = (byte)state.LocalConnectStatuses.Length;
        state.LocalConnectStatuses.CopyTo(inputMessage.PeerConnectStatus);

        ThrowIf.Assert(inputMessage.NumBits <= Max.CompressedBytes * ByteSize.ByteToBits);
        ThrowIf.Assert(lastAckFrame.IsNull || inputMessage.StartFrame == lastAckFrame);

        return messageBodyOverflow
            ? SendInputResult.MessageBodyOverflow
            : SendInputResult.Ok;
    }
}
