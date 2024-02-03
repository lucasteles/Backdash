using System.Globalization;
using System.Runtime.CompilerServices;

namespace nGGPO.Core;

static class ThrowHelpers
{
    public static void ThrowIfArgumentIsNegativeOrZero(int argument,
        [CallerArgumentExpression(nameof(argument))]
        string? paramName = null)
    {
        if (argument <= 0)
            throw new ArgumentOutOfRangeException(argument.ToString(CultureInfo.InvariantCulture), paramName);
    }

    public static void ThrowIfArgumentOutOfBounds(int argument,
        int min = int.MinValue,
        int max = int.MaxValue,
        [CallerArgumentExpression(nameof(argument))]
        string? paramName = null)
    {
        if (argument < min || argument > max)
            throw new ArgumentOutOfRangeException(argument.ToString(CultureInfo.InvariantCulture), paramName);
    }

    public static void ThrowIfTypeArgumentIsReferenceOrContainsReferences<T>()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            throw new InvalidTypeArgumentException<T>(
                "Cannot be used. Only value types without pointers or references are supported."
            );
    }
}
