using System.Diagnostics.CodeAnalysis;

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
    ///     Try loads a <see cref="SavedFrame" /> for <paramref name="frame" />.
    /// </summary>
    /// <returns>true if the frame was found, false otherwise</returns>
    bool TryLoad(in Frame frame, [MaybeNullWhen(false)] out SavedFrame savedFrame);

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
