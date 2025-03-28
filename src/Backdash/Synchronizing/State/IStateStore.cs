namespace Backdash.Synchronizing.State;

/// <summary>
///     Repository for temporary save and restore game states.
/// </summary>
public interface IStateStore
{
    /// <summary>
    ///     Initialize the state buffer with capacity of <paramref name="saveCount" />
    /// </summary>
    /// <param name="saveCount"></param>
    void Initialize(int saveCount);

    /// <summary>
    ///     Returns a <see cref="SavedFrame" /> for <paramref name="frame" />.
    /// </summary>
    /// <param name="frame">Frame to load.</param>
    SavedFrame Load(in Frame frame);

    /// <summary>
    ///     Returns last <see cref="SavedFrame" />.
    /// </summary>
    SavedFrame Last();

    /// <summary>
    ///     Returns next writable <see cref="SavedFrame" />.
    /// </summary>
    ref SavedFrame Next();

    /// <summary>
    ///     Advance the store pointer
    /// </summary>
    void Advance();

    /// <summary>
    ///     Finds checksum for
    ///     <param name="frame"></param>
    /// </summary>
    uint GetChecksum(in Frame frame);
}
