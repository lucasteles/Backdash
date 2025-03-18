using Backdash.Serialization;

namespace Backdash.Synchronizing.State;

/// <summary>
///     Handles <see cref="SessionMode.SyncTest" /> state desync.
/// </summary>
public interface IStateDesyncHandler
{
    /// <summary>
    ///     Handles the states string representations
    /// </summary>
    void Handle(string current, uint currentChecksum, string previous, uint previousChecksum);

    /// <summary>
    ///     Handles the states binary representations
    /// </summary>
    void Handle(
        in BinaryBufferReader current, uint currentChecksum,
        in BinaryBufferReader previous, uint previousChecksum
    );
}
