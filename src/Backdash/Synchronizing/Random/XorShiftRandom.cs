using System.Diagnostics;
using System.Net;

namespace Backdash.Synchronizing.Random;

/// <summary>
/// XOR Shift random number generators (shift-register generators) implementation <seealso cref="IDeterministicRandom"/>
/// </summary>
public sealed class XorShiftRandom : IDeterministicRandom
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
    public void UpdateSeed(int newState, int extraState = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(newState);
        ArgumentOutOfRangeException.ThrowIfNegative(extraState);
        newState = unchecked(newState + extraState + 1);
        seed = (uint)IPAddress.HostToNetworkOrder(newState);
    }
}
