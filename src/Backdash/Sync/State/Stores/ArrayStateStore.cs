using Backdash.Data;

namespace Backdash.Sync.State.Stores;

public class ArrayStateStore<TState> : IStateStore<TState> where TState : notnull
{
    readonly SavedFrame<TState>[] savedStates;

    public ArrayStateStore(RollbackOptions options)
    {
        savedStates = new SavedFrame<TState>[options.PredictionFrames + options.PredictionFramesOffset];
        Array.Fill(savedStates, new SavedFrame<TState>(Frame.Null, default!, 0));
    }

    public void Save(in SavedFrame<TState> state) => throw new NotImplementedException();

    public SavedFrame<TState> Load(Frame frame) => throw new NotImplementedException();

    public SavedFrame<TState> Last() => throw new NotImplementedException();
}
