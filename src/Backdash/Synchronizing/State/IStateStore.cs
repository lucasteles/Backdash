using Backdash.Data;

namespace Backdash.Synchronizing.State;

/// <summary>
/// Repository for temporary save and restore game states.
/// </summary>
/// <typeparam name="TState">Game state type.</typeparam>
public interface IStateStore<TState> : IDisposable where TState : notnull, new()
{
    /// <summary>
    /// Initialize the state buffer with capacity of <paramref name="saveCount"/>
    /// </summary>
    /// <param name="saveCount"></param>
    void Initialize(int saveCount);

    /// <summary>
    /// Returns a <see cref="SavedFrame{TState}" /> for <paramref name="frame"/>.
    /// </summary>
    /// <param name="frame">Frame to load.</param>
    ref readonly SavedFrame<TState> Load(Frame frame);

    /// <summary>
    /// Returns last <see cref="SavedFrame{TState}" />.
    /// </summary>
    ref readonly SavedFrame<TState> Last();

    /// <summary>
    /// Returns current <see cref="SavedFrame{TState}" />.
    /// </summary>
    ref TState GetCurrent();

    /// <summary>
    /// Save current state for <paramref name="frame"/> with <paramref name="checksum"/> value.
    /// </summary>
    /// <param name="frame">frame to save</param>
    /// <param name="checksum">checksum for current state</param>
    /// <returns></returns>
    ref readonly SavedFrame<TState> SaveCurrent(in Frame frame, in int checksum);
}
