using System.Runtime.InteropServices;
using Backdash.Core;
using Backdash.Data;
using Backdash.Serialization;

namespace Backdash.Sync.State.Stores;

using SavedFrameBytes = (Memory<byte> Buffer, int Size);

/// <summary>
/// Binary store for temporary save and restore game states using <see cref="IBinarySerializer{T}"/>.
/// </summary>
/// <param name="serializer">state serializer</param>
/// <param name="hintSize">initial memory used for infer the state size</param>
/// <typeparam name="TState"></typeparam>
public sealed class BinaryStateStore<TState>(
    IBinarySerializer<TState> serializer,
    int hintSize = 128
) : IStateStore<TState> where TState : notnull, new()
{
    int head;
    byte[]? memory;
    SavedFrame<TState>[] savedStates = [];
    SavedFrameBytes[] savedBytes = [];

    /// <inheritdoc />
    public void Initialize(int saveCount)
    {
        savedStates = new SavedFrame<TState>[saveCount];
        for (int i = 0; i < saveCount; i++)
            savedStates[i] = new(Frame.Null, new(), 0);
    }

    /// <inheritdoc />
    public ref TState GetCurrent() => ref savedStates[head].GameState;

    /// <inheritdoc />
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

    /// <inheritdoc />
    public ref readonly SavedFrame<TState> Load(Frame frame)
    {
        for (var i = 0; i < savedStates.Length; i++)
        {
            if (savedStates[i].Frame != frame) continue;
            head = i;
            AdvanceHead();
            return ref Deserialize(i);
        }

        throw new NetcodeException($"Save state not found for frame {frame}");
    }

    /// <inheritdoc />
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
        var bufferSpan = new byte[hintSize];
        var inputSize = FindSize(in saved, ref bufferSpan);
        var saveCount = savedStates.Length;

        memory = Mem.AllocatePinnedArray(inputSize * saveCount);
        savedBytes = new SavedFrameBytes[saveCount];
        for (int i = 0; i < saveCount; i++)
        {
            ref var slot = ref savedBytes[i];
            slot.Size = inputSize;
            slot.Buffer = MemoryMarshal.CreateFromPinnedArray(memory, i * inputSize, inputSize);
        }

        ref var first = ref savedBytes[0];
        bufferSpan[..inputSize].CopyTo(first.Buffer.Span);
        first.Size = inputSize;
    }

    int FindSize(in TState saved, ref byte[] bufferSpan)
    {
        try
        {
            if (bufferSpan.Length > ByteSize.FromMegaBytes(1).ByteCount)
                throw new InvalidOperationException("Game state is too large.");

            var inputSize = serializer.Serialize(in saved, bufferSpan);
            return inputSize;
        }
        catch (Exception e)
        {
            if (e is IndexOutOfRangeException or ArgumentOutOfRangeException ||
                (e is ArgumentException arg && arg.Message.Contains("too short")))
            {
                Array.Resize(ref bufferSpan, bufferSpan.Length * 2);
                return FindSize(in saved, ref bufferSpan);
            }

            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose() => memory = null!;
}
