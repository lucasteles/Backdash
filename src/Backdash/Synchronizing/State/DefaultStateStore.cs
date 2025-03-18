using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Core;
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
    public SavedFrame Load(in Frame frame)
    {
        for (var i = 0; i < savedStates.Length; i++)
        {
            if (savedStates[i].Frame.Number != frame.Number) continue;
            head = i;
            Advance();
            return savedStates[i];
        }

        throw new NetcodeException($"Save state not found for frame {frame.Number}");
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
