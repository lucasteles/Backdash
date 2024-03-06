using System.Globalization;
using System.Runtime.CompilerServices;

namespace Backdash.Core;

static class ThrowHelpers
{
    public static void ThrowIfArgumentOutOfBounds(int argument,
        int min = int.MinValue,
        int max = int.MaxValue,
        [CallerArgumentExpression(nameof(argument))]
        string? paramName = null)
    {
        if (argument < min || argument > max)
            throw new ArgumentOutOfRangeException(argument.ToString(CultureInfo.InvariantCulture), paramName);
    }

    public static void ThrowIfArgumentIsZeroOrLess(int argument,
        [CallerArgumentExpression(nameof(argument))]
        string? paramName = null)
    {
        if (argument <= 0)
            throw new ArgumentOutOfRangeException(argument.ToString(CultureInfo.InvariantCulture), paramName);
    }

    public static void ThrowIfArgumentIsNegative(int argument,
        [CallerArgumentExpression(nameof(argument))]
        string? paramName = null)
    {
        if (argument < 0)
            throw new ArgumentOutOfRangeException(argument.ToString(CultureInfo.InvariantCulture), paramName);
    }

    public static void ThrowIfTypeTooBigForStack<T>() where T : struct
    {
        var size = Mem.SizeOf<T>();
        if (size > Mem.MaxStackLimit)
            throw new BackdashException($"{typeof(T).Name} size too big for stack: {size}");
    }

    public static void ThrowIfTypeIsReferenceOrContainsReferences<T>() where T : struct
    {
        if (Mem.IsReferenceOrContainsReferences<T>())
            throw new ArgumentException("Input struct must not have reference type members");
    }
}
