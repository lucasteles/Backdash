using Backdash.Serialization;

namespace Backdash.Synchronizing.State;

/// <summary>
/// Handle Sync Test state mismatches
/// Tip: useful for smart state comparisons
/// </summary>
public interface IStateDesyncHandler
{
    /// <summary>
    /// Handles the states string representations
    /// </summary>
    void Handle(string current, uint currentChecksum, string previous, uint previousChecksum);

    /// <summary>
    /// Handles the states binary representations
    /// </summary>
    void Handle(
        in BinaryBufferReader current, uint currentChecksum,
        in BinaryBufferReader previous, uint previousChecksum
    );
}
