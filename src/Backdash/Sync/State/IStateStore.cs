using Backdash.Data;

namespace Backdash.Sync.State;

public interface IStateStore<TState> : IDisposable where TState : notnull
{
    void Initialize(int size);
    void Save(in SavedFrame<TState> state);
    SavedFrame<TState> Load(Frame frame);
    SavedFrame<TState> Last();
}
