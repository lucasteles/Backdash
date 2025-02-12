using Backdash.Serialization.Buffer;

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
    void Handle(string current, int currentChecksum, string previous, int previousChecksum);

    /// <summary>
    /// Handles the states binary representations
    /// </summary>
    void Handle(
        in BinaryBufferReader current, int currentChecksum,
        in BinaryBufferReader previous, int previousChecksum
    );
}
