using Backdash.Core;
using Backdash.Data;

namespace Backdash.Synchronizing.State.Stores;

/// <summary>
/// Array pool store for temporary save and restore game states.
/// </summary>
public sealed class ArrayStateStore<TState> : IStateStore<TState> where TState : notnull, new()
{
    SavedFrame<TState>[] savedStates = [];
    int head;

    /// <inheritdoc />
    public void Initialize(int saveCount)
    {
        savedStates = GC.AllocateArray<SavedFrame<TState>>(saveCount, pinned: true);
        for (int i = 0; i < saveCount; i++)
            savedStates[i] = new(Frame.Null, new(), 0);
    }

    /// <inheritdoc />
    public ref readonly SavedFrame<TState> Load(Frame frame)
    {
        for (var i = 0; i < savedStates.Length; i++)
        {
            ref var current = ref savedStates[i];
            if (current.Frame.Number != frame.Number) continue;
            head = i;
            AdvanceHead();
            return ref current;
        }

        throw new NetcodeException($"Save state not found for frame {frame}");
    }

    /// <inheritdoc />
    public ref readonly SavedFrame<TState> Last()
    {
        var i = head - 1;
        if (i < 0)
            return ref savedStates[^1];
        return ref savedStates[i];
    }

    /// <inheritdoc />
    public ref TState GetCurrent() => ref savedStates[head].GameState;

    /// <inheritdoc />
    public ref readonly SavedFrame<TState> SaveCurrent(in Frame frame, in int checksum)
    {
        ref var current = ref savedStates[head];
        current.Frame = frame;
        current.Checksum = checksum;
        AdvanceHead();
        return ref current;
    }

    void AdvanceHead() => head = (head + 1) % savedStates.Length;

    /// <inheritdoc />
    public void Dispose() { }
}
