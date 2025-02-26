using System.Globalization;
using System.Runtime.CompilerServices;

namespace Backdash.Core;

static class ThrowHelpers
{
    public static void ThrowIfArgumentOutOfBounds(
        int argument,
        int min = int.MinValue,
        int max = int.MaxValue,
        [CallerArgumentExpression(nameof(argument))]
        string? paramName = null)
    {
        if (argument < min || argument > max)
            throw new ArgumentOutOfRangeException(argument.ToString(CultureInfo.InvariantCulture), paramName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfTypeIsReferenceOrContainsReferences<T>() where T : struct
    {
        if (Mem.IsReferenceOrContainsReferences<T>())
            throw new ArgumentException($"Type {typeof(T).FullName} must not have reference type members");
    }
}
