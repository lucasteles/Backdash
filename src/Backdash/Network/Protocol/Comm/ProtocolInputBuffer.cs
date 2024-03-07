using System.Diagnostics;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Messages;
using Backdash.Serialization;
using Backdash.Sync;
using Backdash.Sync.Input;
namespace Backdash.Network.Protocol.Comm;
interface IProtocolInputBuffer<TInput> where TInput : struct
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
    where TInput : struct
{
    readonly Queue<GameInput<TInput>> pendingOutput;
    readonly Memory<byte> workingBufferMemory;
    int lastSentSize;
    int lastAckSize;
    GameInput<TInput> lastAckedInput = new();
    public GameInput<TInput> LastSent { get; private set; } = new();
    readonly int inputSize;
    readonly ProtocolOptions options;
    readonly IBinaryWriter<TInput> inputSerializer;
    readonly ProtocolState state;
    readonly Logger logger;
    readonly ITimeSync<TInput> timeSync;
    readonly IMessageSender sender;
    readonly IProtocolInbox<TInput> inbox;
    public int PendingNumber => pendingOutput.Count;
    public ProtocolInputBuffer(ProtocolOptions options,
        IBinaryWriter<TInput> inputSerializer,
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
        inputSize = inputSerializer.GetTypeSize();
        Trace.Assert(inputSize * ByteSize.ByteToBits < 1 << ByteSize.ByteToBits);
        workingBufferMemory = Mem.CreatePinnedMemory(WorkingBufferFactor * inputSize);
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
        var createMessageResult = CreateInputMessage(out var inputMessage);
        sender.SendMessage(in inputMessage);
        return createMessageResult;
    }
    SendInputResult CreateInputMessage(out ProtocolMessage protocolMessage)
    {
        Span<byte> workingBuffer = workingBufferMemory.Span;
        Trace.Assert(workingBuffer.Length >= WorkingBufferFactor);
        Span<byte> lastAckBytes = workingBuffer[..inputSize];
        Span<byte> sendBuffer = workingBuffer.Slice(inputSize, inputSize);
        Span<byte> currentBytes = workingBuffer.Slice(inputSize * 2, inputSize);
        var messageBodyOverflow = false;
        var lastAckFrame = inbox.LastAckedFrame;
        InputMessage inputMessage = new()
        {
            StartFrame = Frame.Zero,
        };
        if (pendingOutput.Count > 0)
        {
            while (pendingOutput.Peek().Frame < lastAckFrame)
            {
                var acked = pendingOutput.Dequeue();
                logger.Write(LogLevel.Debug, $"Skipping past frame:{acked.Frame} current is {lastAckFrame}");
                lastAckedInput = acked;
                lastAckSize = inputSerializer.Serialize(in lastAckedInput.Data, lastAckBytes);
            }
            Trace.Assert(lastAckedInput.Frame.IsNull || lastAckedInput.Frame.Next() == lastAckFrame);
            var current = pendingOutput.Peek();
            var currentSize = inputSerializer.Serialize(in current.Data, currentBytes);
            inputMessage.InputSize = (byte)currentSize;
            inputMessage.StartFrame = current.Frame;
            Trace.Assert(lastAckedInput.Frame.IsNull || lastAckedInput.Frame.Next() == inputMessage.StartFrame);
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
        state.LocalConnectStatuses.CopyTo(inputMessage.PeerConnectStatus);
        Trace.Assert(inputMessage.NumBits <= Max.CompressedBytes * ByteSize.ByteToBits);
        Trace.Assert(lastAckFrame.IsNull || inputMessage.StartFrame == lastAckFrame);
        protocolMessage = new(MessageType.Input)
        {
            Input = inputMessage,
        };
        return messageBodyOverflow
            ? SendInputResult.MessageBodyOverflow
            : SendInputResult.Ok;
    }
}
