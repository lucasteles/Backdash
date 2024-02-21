using Backdash.Data;

namespace Backdash.Sync.State;

public record struct SavedFrame<TState>(Frame Frame, TState GameState, int Checksum)
    where TState : notnull
{
    public Frame Frame = Frame;
    public TState GameState = GameState;
    public int Checksum = Checksum;
}
