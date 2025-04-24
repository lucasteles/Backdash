using Backdash.Serialization;

namespace Backdash.Synchronizing.State;

/// <summary>
///     Handles <see cref="SessionMode.SyncTest" /> state desync.
/// </summary>
public interface IStateDesyncHandler
{
    /// <summary>
    ///     Handles the states binary representations
    /// </summary>
    void Handle(in StateSnapshot previous, in StateSnapshot current);
}

/// <summary>
///  State desync snapshot
/// </summary>
public readonly ref struct StateSnapshot(
    string value,
    ref readonly BinaryBufferReader reader,
    uint checksum,
    object? state
)
{
    /// <summary>State text representation</summary>
    public readonly string Value = value;

    /// <summary>State object value</summary>
    /// <seealso cref="INetcodeSessionHandler.ParseState"/>
    public readonly object? State = state;

    /// <summary>State binary reader</summary>
    public readonly BinaryBufferReader Reader = reader;

    /// <summary>State checksum value</summary>
    public readonly uint Checksum = checksum;

    /// <inheritdoc />
    public override string ToString() => Value;
}
