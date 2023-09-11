using System.Globalization;
using System.Runtime.CompilerServices;

namespace Backdash.Core;

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

    public static void ThrowIfTypeSizeGreaterThan<T>(int maxSize) where T : struct
    {
        ThrowIfArgumentIsNegativeOrZero(maxSize);
        var size = Mem.SizeOf<T>();
        if (size > maxSize)
            throw new BackdashException($"{typeof(T).Name} is too big {size}, max: {maxSize}");
    }

    public static void ThrowIfTypeTooBigForStack<T>() where T : struct
    {
        var size = Mem.SizeOf<T>();
        if (size <= Mem.MaxStackLimit)
            throw new BackdashException($"{typeof(T).Name} size too big for stack: {size}");
    }
}
