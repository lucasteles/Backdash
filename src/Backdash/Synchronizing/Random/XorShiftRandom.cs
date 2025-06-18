using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Backdash.Synchronizing.Random;

/// <summary>
///     XOR Shift random number generators (shift-register generators) implementation
///     <seealso cref="IDeterministicRandom{T}" />
/// </summary>
public sealed class XorShiftRandom<TInput> : IDeterministicRandom<TInput> where TInput : unmanaged
{
    uint state;

    /// <inheritdoc />
    public uint InitialSeed { get; private set; }

    /// <inheritdoc />
    public uint CurrentSeed { get; private set; }

    /// <inheritdoc />
    public uint CurrentState => state;

    /// <inheritdoc />
    public uint Next()
    {
        Debug.Assert(state > 0);
        var x = state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        return state = x;
    }

    /// <inheritdoc />
    public void SetInitialSeed(uint value) => InitialSeed = value;

    /// <inheritdoc />
    public void UpdateSeed(in Frame currentFrame, ReadOnlySpan<TInput> inputs, uint extraState = 0)
    {
        unchecked
        {
            var offset = currentFrame.Number % 31;
            var inputSeed = MathI.SumRaw(MemoryMarshal.Cast<TInput, uint>(inputs)) << offset;
            var newSeed = InitialSeed + (uint)currentFrame.Number + inputSeed + extraState + 1;
            if (BitConverter.IsLittleEndian)
                newSeed = BinaryPrimitives.ReverseEndianness(newSeed);
            state = CurrentSeed = newSeed;
        }
    }
}
