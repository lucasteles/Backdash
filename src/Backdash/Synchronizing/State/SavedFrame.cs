using System.Buffers;
using Backdash.Data;

namespace Backdash.Synchronizing.State;

/// <summary>
///     Represents a save-state at specific frame.
/// </summary>
/// <param name="Frame">Saved frame number</param>
/// <param name="GameState">Game state on <paramref name="Frame" /></param>
/// <param name="Checksum">Checksum of state</param>
public sealed record SavedFrame(Frame Frame, ArrayBufferWriter<byte> GameState, uint Checksum)
{
    /// <summary>Saved frame number</summary>
    public Frame Frame = Frame;

    /// <summary>Saved checksum</summary>
    public uint Checksum = Checksum;

    /// <summary>Saved game state</summary>
    public ArrayBufferWriter<byte> GameState = GameState;

    /// <summary>Saved state size</summary>
    public ByteSize Size => ByteSize.FromBytes(GameState.WrittenCount);
}
