using System.Diagnostics;
using System.Net;
using Backdash.Core;

namespace Backdash.Synchronizing.Random;

/// <summary>
/// Xorshift random number generators (shift-register generators) implementation <seealso cref="IDeterministicRandom"/>
/// </summary>
public sealed class XorShiftRandom : IDeterministicRandom
{
    uint state;

    /// <inheritdoc />
    public uint Next()
    {
        Debug.Assert(state > 0);

        uint x = state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        state = x;
        return x;
    }

    /// <inheritdoc />
    public void Reseed(int newState)
    {
        ThrowHelpers.ThrowIfArgumentIsNegative(newState);
        state = (uint)IPAddress.HostToNetworkOrder(newState + 1);
    }
}
