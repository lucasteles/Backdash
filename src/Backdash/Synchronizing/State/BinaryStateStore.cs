using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Core;
using Backdash.Data;
using Backdash.Serialization;

namespace Backdash.Synchronizing.State;

/// <summary>
/// Binary store for temporary save and restore game states using <see cref="IBinarySerializer{T}"/>.
/// </summary>
/// <param name="hintSize">initial memory used for infer the state size</param>
public sealed class BinaryStateStore(int hintSize = 128) : IStateStore
{
    int head;
    SavedFrame[] savedStates = [];

    /// <inheritdoc />
    public void Initialize(int saveCount)
    {
        savedStates = new SavedFrame[saveCount];
        for (int i = 0; i < saveCount; i++)
            savedStates[i] = new(Frame.Null, new(hintSize), 0);
    }

    /// <inheritdoc />
    public ref SavedFrame GetCurrent()
    {
        ref var result = ref savedStates[head];
        result.GameState.Clear();
        return ref result!;
    }

    /// <inheritdoc />
    public SavedFrame Load(Frame frame)
    {
        for (var i = 0; i < savedStates.Length; i++)
        {
            if (savedStates[i].Frame != frame) continue;
            head = i;
            Advance();
            return savedStates[i];
        }

        throw new NetcodeException($"Save state not found for frame {frame}");
    }

    /// <inheritdoc />
    public SavedFrame Last()
    {
        var index = LastIndex();
        return savedStates[index];

        int LastIndex()
        {
            var i = head - 1;
            if (i < 0)
                return savedStates.Length - 1;
            return i;
        }
    }

    /// <inheritdoc />
    public void Advance() => head = (head + 1) % savedStates.Length;

    /// <inheritdoc />
    public int GetChecksum(in Frame frame)
    {
        ref var current = ref MemoryMarshal.GetReference(savedStates.AsSpan());
        ref var limit = ref Unsafe.Add(ref current, savedStates.Length);

        while (Unsafe.IsAddressLessThan(in current, in limit))
        {
            if (current.Frame.Number == frame.Number)
                return current.Checksum;

            current = ref Unsafe.Add(ref current, 1)!;
        }

        return 0;
    }
}
