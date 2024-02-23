using Backdash.Core;
using Backdash.Data;

namespace Backdash.Sync.State.Stores;

public sealed class ArrayStateStore<TState> : IStateStore<TState> where TState : notnull, new()
{
    SavedFrame<TState>[] savedStates = [];
    int head;

    public void Initialize(int size)
    {
        savedStates = new SavedFrame<TState>[size];
        Array.Fill(savedStates, new SavedFrame<TState>(Frame.Null, new TState(), 0));
    }

    public ref readonly SavedFrame<TState> Load(Frame frame)
    {
        for (var i = 0; i < savedStates.Length; i++)
        {
            ref var current = ref savedStates[i];
            if (current.Frame != frame) continue;
            head = i;
            AdvanceHead();
            return ref current;
        }

        throw new BackdashException($"Save state not found for frame {frame}");
    }

    public ref readonly SavedFrame<TState> Last()
    {
        var i = head - 1;
        if (i < 0)
            return ref savedStates[^1];

        return ref savedStates[i];
    }

    public ref TState GetCurrent() => ref savedStates[head].GameState;

    public ref readonly SavedFrame<TState> SaveCurrent(in Frame frame, in int checksum)
    {
        ref var current = ref savedStates[head];
        current.Frame = frame;
        current.Checksum = checksum;
        AdvanceHead();
        return ref current;
    }

    void AdvanceHead() => head = (head + 1) % savedStates.Length;

    public void Dispose() { }
}
