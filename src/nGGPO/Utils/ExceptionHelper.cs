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
            throw new ArgumentException(argument.ToString(CultureInfo.InvariantCulture), paramName);
    }
}
