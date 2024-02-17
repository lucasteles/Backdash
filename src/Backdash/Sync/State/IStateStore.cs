using Backdash.Data;

namespace Backdash.Sync.State;

public interface IStateStore<TState> where TState : notnull
{
    void Save(in SavedFrame<TState> state);
    SavedFrame<TState> Load(Frame frame);
    SavedFrame<TState> Last();
}
