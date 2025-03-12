using System.Buffers.Binary;
using System.Diagnostics;
using Backdash.Core;
using Backdash.Data;

namespace Backdash.Synchronizing.Random;

/// <summary>
/// XOR Shift random number generators (shift-register generators) implementation <seealso cref="IDeterministicRandom{T}"/>
/// </summary>
public sealed class XorShiftRandom<TInput> : IDeterministicRandom<TInput> where TInput : unmanaged
{
    uint seed;

    /// <inheritdoc />
    public uint Next()
    {
        unchecked
        {
            Debug.Assert(seed > 0);
            var x = seed;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return seed = x;
        }
    }

    /// <inheritdoc />
    public void UpdateSeed(in Frame currentFrame, ReadOnlySpan<TInput> inputs)
    {
        var extraState = Mem.PopCount(inputs);
        seed = unchecked((uint)(currentFrame.Number + extraState + 1));
        if (BitConverter.IsLittleEndian)
            seed = BinaryPrimitives.ReverseEndianness(seed);
    }
}
