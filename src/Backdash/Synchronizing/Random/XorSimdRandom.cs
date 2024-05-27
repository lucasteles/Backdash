using System.Net;
using System.Numerics;
using Backdash.Core;

namespace Backdash.Synchronizing.Random;

/// <summary>
/// XOR Simd random number generators (shift-register generators) implementation
/// <seealso cref="IDeterministicRandom"/>
/// </summary>
public sealed class XorSimdRandom : IDeterministicRandom
{
    Vector<uint> state0;
    Vector<uint> state1;

    /// <inheritdoc />
    public uint Next()
    {
        Vector<uint> s1 = state0;
        Vector<uint> s0 = state1;
        state0 = s0;
        s1 ^= s1 << 23;
        state1 = s1 ^ s0 ^ (s1 >> 17) ^ (s0 >> 26);
        return Vector.Xor(state1 + s0, state0)[0];
    }

    /// <inheritdoc />
    public void UpdateSeed(int newState, int extraState = 0)
    {
        ThrowHelpers.ThrowIfArgumentIsNegative(newState);
        ThrowHelpers.ThrowIfArgumentIsNegative(extraState);

        if (extraState is 0)
            extraState = unchecked(1812433253 * (newState ^ (newState >> 30)));

        state0 = new((uint)IPAddress.HostToNetworkOrder(newState + 1));
        state1 = new((uint)IPAddress.HostToNetworkOrder(extraState + 1));
    }
}
