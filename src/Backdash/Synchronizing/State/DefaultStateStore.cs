using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Serialization;

namespace Backdash.Synchronizing.State;

/// <summary>
///     Binary store for temporary save and restore game states using <see cref="IBinarySerializer{T}" />.
/// </summary>
/// <param name="hintSize">initial memory used for infer the state size</param>
public sealed class DefaultStateStore(int hintSize) : IStateStore
{
    int head;
    SavedFrame[] savedStates = [];

    /// <inheritdoc />
    public void Initialize(int saveCount)
    {
        savedStates = new SavedFrame[saveCount];
        for (var i = 0; i < saveCount; i++)
            savedStates[i] = new(Frame.Null, new(hintSize), 0);
    }

    /// <inheritdoc />
    public ref SavedFrame Next()
    {
        ref var result = ref savedStates[head];
        result.GameState.ResetWrittenCount();
        return ref result!;
    }

    /// <inheritdoc />
    public bool TryLoad(in Frame frame, [MaybeNullWhen(false)] out SavedFrame savedFrame)
    {
        var i = 0;
        var span = savedStates.AsSpan();
        ref var current = ref MemoryMarshal.GetReference(span);
        ref var limit = ref Unsafe.Add(ref current, span.Length);

        while (Unsafe.IsAddressLessThan(in current, in limit))
        {
            if (current.Frame.Number == frame.Number)
            {
                head = i;
                Advance();
                savedFrame = current;
                return true;
            }

            i++;
            current = ref Unsafe.Add(ref current, 1)!;
        }

        savedFrame = null;
        return false;
    }

    /// <inheritdoc />
    public SavedFrame Last()
    {
        var i = head - 1;
        var index = i < 0 ? savedStates.Length - 1 : i;
        return savedStates[index];
    }

    /// <inheritdoc />
    public void Advance() => head = (head + 1) % savedStates.Length;

    /// <inheritdoc />
    public uint GetChecksum(in Frame frame)
    {
        var span = savedStates.AsSpan();
        ref var current = ref MemoryMarshal.GetReference(span);
        ref var limit = ref Unsafe.Add(ref current, span.Length);

        while (Unsafe.IsAddressLessThan(in current, in limit))
        {
            if (current.Frame.Number == frame.Number)
                return current.Checksum;

            current = ref Unsafe.Add(ref current, 1)!;
        }

        return 0;
    }
}
