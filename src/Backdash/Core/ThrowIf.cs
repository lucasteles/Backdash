using System.Globalization;
using System.Runtime.CompilerServices;

namespace Backdash.Core;

static class ThrowIf
{
    public static void ArgumentOutOfBounds(
        int argument,
        int min = int.MinValue,
        int max = int.MaxValue,
        [CallerArgumentExpression(nameof(argument))]
        string? paramName = null)
    {
        if (argument < min || argument > max)
            throw new ArgumentOutOfRangeException(argument.ToString(CultureInfo.InvariantCulture), paramName);
    }

    public static void InvalidEnum<T>(
        T value,
        [CallerArgumentExpression(nameof(value))]
        string? argName = null
    ) where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentOutOfRangeException($"Invalid Enum of type {typeof(T).Name} for {argName}: {value}");
    }

    public static void Assert(
        bool condition,
        string? info = null,
        [CallerArgumentExpression(nameof(condition))]
        string? paramName = null,
        [CallerFilePath] string? location = null,
        [CallerLineNumber] int line = 0
    )
    {
        if (!condition)
            throw new NetcodeAssertionException(
                $"False assertion {paramName}: {info ?? "expected true, got false"} in {location}:{line}");
    }
}
