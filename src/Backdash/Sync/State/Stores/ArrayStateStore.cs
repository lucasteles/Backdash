using Backdash.Core;
using Backdash.Data;

namespace Backdash.Sync.State.Stores;

public class ArrayStateStore<TState> : IStateStore<TState> where TState : notnull
{
    SavedFrame<TState>[] savedStates = [];
    int head;

    public void Initialize(int size)
    {
        savedStates = new SavedFrame<TState>[size];
        Array.Fill(savedStates, new SavedFrame<TState>(Frame.Null, default!, 0));
    }

    public void Save(in SavedFrame<TState> state)
    {
        savedStates[head] = state;
        AdvanceHead();
    }

    public ref readonly SavedFrame<TState> Load(Frame frame) => ref FindSavedFrame(frame, setHead: true);

    public ref readonly SavedFrame<TState> Last()
    {
        var i = head - 1;
        if (i < 0)
            return ref savedStates[^1];

        return ref savedStates[i];
    }

    void AdvanceHead() => head = (head + 1) % savedStates.Length;

    ref readonly SavedFrame<TState> FindSavedFrame(Frame frame, bool setHead = false)
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
}
