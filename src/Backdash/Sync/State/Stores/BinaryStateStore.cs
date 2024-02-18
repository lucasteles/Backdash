using System.Buffers;
using System.Runtime.InteropServices;
using Backdash.Core;
using Backdash.Data;
using Backdash.Serialization;

namespace Backdash.Sync.State.Stores;

public sealed class BinaryStateStore<TState>(
    IBinarySerializer<TState> serializer
) : IStateStore<TState> where TState : notnull
{
    int head;
    int saveCount = Default.MaxInputQueue;

    byte[]? memory;
    BinarySavedFrame[] savedStates = null!;

    public void Initialize(int size) => saveCount = size;

    public void Save(in SavedFrame<TState> state)
    {
        if (memory is null)
            AllocateResources(state);
        else
        {
            var gameState = state.GameState;
            ref var next = ref savedStates[head];
            next.Size = serializer.Serialize(ref gameState, next.GameState.Span);
            next.Frame = state.Frame;
            next.Checksum = state.Checksum;
        }

        AdvanceHead();
    }

    public SavedFrame<TState> Load(Frame frame)
    {
        ref readonly var saved = ref FindSavedFrame(frame, setHead: true);
        return Deserialize(in saved);
    }

    public SavedFrame<TState> Last()
    {
        ref readonly var last = ref LastBinary();
        return Deserialize(in last);

        ref BinarySavedFrame LastBinary()
        {
            var i = head - 1;
            if (i < 0)
                return ref savedStates[^1];

            return ref savedStates[i];
        }
    }

    SavedFrame<TState> Deserialize(in BinarySavedFrame saved) =>
        new()
        {
            Frame = saved.Frame,
            Checksum = saved.Checksum,
            GameState = serializer.Deserialize(saved.GameState.Span[..saved.Size]),
        };

    void AdvanceHead() => head = (head + 1) % savedStates.Length;

    ref readonly BinarySavedFrame FindSavedFrame(Frame frame, bool setHead = false)
    {
        for (var i = 0; i < savedStates.Length; i++)
        {
            ref var current = ref savedStates[i];
            if (current.Frame != frame) continue;

            if (setHead)
            {
                head = i;
                AdvanceHead();
            }

            return ref current;
        }

        throw new BackdashException($"Invalid state frame search: {frame}");
    }


    void AllocateResources(SavedFrame<TState> saved)
    {
        ArrayBufferWriter<byte> bufferWriter = new();
        var bufferSpan = bufferWriter.GetSpan();
        var inputSize = serializer.Serialize(ref saved.GameState, bufferSpan);

        memory = GC.AllocateArray<byte>(inputSize * saveCount, pinned: true);
        savedStates = new BinarySavedFrame[saveCount];
        for (int i = 0; i < saveCount; i++)
        {
            ref var slot = ref savedStates[i];
            slot.Frame = Frame.Null;
            slot.Size = bufferWriter.WrittenCount;
            slot.GameState = MemoryMarshal.CreateFromPinnedArray(memory, i * inputSize, inputSize);
        }

        ref var first = ref savedStates[0];

        bufferSpan[..inputSize].CopyTo(first.GameState.Span);
        first.Size = inputSize;
        first.Frame = saved.Frame;
        first.Checksum = saved.Checksum;
    }

    public void Dispose() => memory = null!;

    record struct BinarySavedFrame
    {
        public Frame Frame;
        public Memory<byte> GameState;
        public int Size;
        public int Checksum;
    }
}
