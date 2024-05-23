using Backdash.Core;

namespace Backdash.Synchronizing.Random;

/// <summary>
/// Defines a random number generator
/// </summary>
public interface ISessionRandom
{
    /// <summary>
    /// Returns a random unsigned integer.
    /// </summary>
    uint Next();

    /// <summary>
    /// Returns a random non-negative integer.
    /// </summary>
    int NextInt()
    {
        while (true)
        {
            var result = Next() >> 1;
            if (result is not int.MaxValue)
                return (int)result;
        }
    }

    /// <summary>
    /// Returns a random integer that is within a specified range.
    /// </summary>
    int NextInt(int minValue, int maxValue)
    {
        if (minValue >= maxValue)
            throw new ArgumentOutOfRangeException(nameof(minValue), "minValue must be less than maxValue");

        uint range = (uint)(maxValue - minValue);
        return (int)(Next() % range) + minValue;
    }

    /// <summary>
    /// Returns a random integer that is between 0 and <paramref name="maxValue"/>
    /// </summary>
    int NextInt(int maxValue)
    {
        ThrowHelpers.ThrowIfArgumentIsNegative(maxValue);
        return NextInt(0, maxValue);
    }

    /// <summary>
    /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
    /// </summary>
    float NextFloat() => (float)Next() / uint.MaxValue;
}

/// <summary>
/// Defines a random number generator with mutable state
/// </summary>
public interface IDeterministicRandom : ISessionRandom
{
    /// <summary>
    /// Updates the seed for the current random instance
    /// </summary>
    /// <param name="newState"></param>
    void Reseed(int newState);
}
