using Backdash.Data;
namespace Backdash.Sync.State;
public interface IStateStore<TState> : IDisposable where TState : notnull, new()
{
    void Initialize(int size);
    ref readonly SavedFrame<TState> Load(Frame frame);
    ref readonly SavedFrame<TState> Last();
    ref TState GetCurrent();
    ref readonly SavedFrame<TState> SaveCurrent(in Frame frame, in int checksum);
}
