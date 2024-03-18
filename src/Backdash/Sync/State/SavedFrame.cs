using Backdash.Data;

namespace Backdash.Sync.State;

/// <summary>
/// Represents a save-state at specific frame.
/// </summary>
/// <param name="Frame">Saved frame number</param>
/// <param name="GameState">Game state on <paramref name="Frame"/></param>
/// <param name="Checksum">Checksum of state</param>
/// <typeparam name="TState">Game state type</typeparam>
public record struct SavedFrame<TState>(Frame Frame, TState GameState, int Checksum)
    where TState : notnull
{
    /// <summary>Saved frame number</summary>
    public Frame Frame = Frame;

    /// <summary>Saved game state</summary>
    public TState GameState = GameState;

    /// <summary>Saved checksum</summary>
    public int Checksum = Checksum;
}
