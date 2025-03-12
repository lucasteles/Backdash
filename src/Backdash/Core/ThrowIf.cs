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

    public static void Assert(
        bool condition,
        string? info = null,
        [CallerArgumentExpression(nameof(condition))]
        string? paramName = null
    )
    {
        if (!condition)
            throw new InvalidOperationException(
                $"False assertion on {paramName}: {info ?? "expected true, got false"}");
    }
}
