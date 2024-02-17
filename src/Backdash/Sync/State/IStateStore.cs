using Backdash.Data;

namespace Backdash.Sync.State;

public interface IStateStore<TState> where TState : notnull
{
    void Initialize(int size);
    void Save(in SavedFrame<TState> state);
    ref readonly SavedFrame<TState> Load(Frame frame);
    ref readonly SavedFrame<TState> Last();
}
