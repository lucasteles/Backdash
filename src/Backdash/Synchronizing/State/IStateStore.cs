using Backdash.Data;

namespace Backdash.Synchronizing.State;

/// <summary>
/// Repository for temporary save and restore game states.
/// </summary>
public interface IStateStore : IDisposable
{
    /// <summary>
    /// Initialize the state buffer with capacity of <paramref name="saveCount"/>
    /// </summary>
    /// <param name="saveCount"></param>
    void Initialize(int saveCount);

    /// <summary>
    /// Returns a <see cref="SavedFrame" /> for <paramref name="frame"/>.
    /// </summary>
    /// <param name="frame">Frame to load.</param>
    SavedFrame Load(Frame frame);

    /// <summary>
    /// Returns last <see cref="SavedFrame" />.
    /// </summary>
    SavedFrame Last();

    /// <summary>
    /// Returns current <see cref="SavedFrame" />.
    /// </summary>
    ref SavedFrame GetCurrent();

    /// <summary>
    /// Advance the store pointer
    /// </summary>
    void Advance();
}
