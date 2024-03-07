using System.Buffers;
using System.Runtime.InteropServices;
using Backdash.Core;
using Backdash.Data;
using Backdash.Serialization;
namespace Backdash.Sync.State.Stores;
using SavedFrameBytes = (Memory<byte> Buffer, int Size);
public sealed class BinaryStateStore<TState>(
    IBinarySerializer<TState> serializer
) : IStateStore<TState> where TState : notnull, new()
{
    int head;
    byte[]? memory;
    SavedFrame<TState>[] savedStates = [];
    SavedFrameBytes[] savedBytes = [];
    public void Initialize(int size)
    {
        savedStates = new SavedFrame<TState>[size];
        for (int i = 0; i < size; i++)
            savedStates[i] = new(Frame.Null, new TState(), 0);
    }
    public ref TState GetCurrent() => ref savedStates[head].GameState;
    public ref readonly SavedFrame<TState> SaveCurrent(in Frame frame, in int checksum)
    {
        ref var current = ref savedStates[head];
        current.Frame = frame;
        current.Checksum = checksum;
        if (memory is null)
            AllocateResources(in current.GameState);
        else
        {
            ref var bytes = ref savedBytes[head];
            bytes.Size = serializer.Serialize(in current.GameState, bytes.Buffer.Span);
        }
        AdvanceHead();
        return ref current;
    }
    public ref readonly SavedFrame<TState> Load(Frame frame)
    {
        for (var i = 0; i < savedStates.Length; i++)
        {
            if (savedStates[i].Frame != frame) continue;
            head = i;
            AdvanceHead();
            return ref Deserialize(i);
        }
        throw new BackdashException($"Save state not found for frame {frame}");
    }
    public ref readonly SavedFrame<TState> Last()
    {
        var index = LastIndex();
        return ref Deserialize(index);
        int LastIndex()
        {
            var i = head - 1;
            if (i < 0)
                return savedStates.Length - 1;
            return i;
        }
    }
    ref SavedFrame<TState> Deserialize(int index)
    {
        ref var frame = ref savedStates[index];
        ref var bytes = ref savedBytes[index];
        serializer.Deserialize(bytes.Buffer.Span[..bytes.Size], ref frame.GameState);
        return ref frame;
    }
    void AdvanceHead() => head = (head + 1) % savedStates.Length;
    void AllocateResources(in TState saved)
    {
        ArrayBufferWriter<byte> bufferWriter = new();
        var bufferSpan = bufferWriter.GetSpan();
        var inputSize = serializer.Serialize(in saved, bufferSpan);
        var saveCount = savedStates.Length;
        memory = Mem.AllocatePinnedArray(inputSize * saveCount);
        savedBytes = new SavedFrameBytes[saveCount];
        for (int i = 0; i < saveCount; i++)
        {
            ref var slot = ref savedBytes[i];
            slot.Size = bufferWriter.WrittenCount;
            slot.Buffer = MemoryMarshal.CreateFromPinnedArray(memory, i * inputSize, inputSize);
        }
        ref var first = ref savedBytes[0];
        bufferSpan[..inputSize].CopyTo(first.Buffer.Span);
        first.Size = inputSize;
    }
    public void Dispose() => memory = null!;
}
