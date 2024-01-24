using System.Globalization;
using System.Runtime.CompilerServices;

namespace nGGPO.Utils;

static class ExceptionHelper
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
}
