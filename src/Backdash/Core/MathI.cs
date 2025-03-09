using System.Runtime.CompilerServices;

namespace Backdash;

/// <summary>
/// Int Math
/// </summary>
public static class MathI
{
    /// <summary>
    ///  Divide two integers ceiling the result
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CeilDiv(int x, int y) => x is 0 ? 0 : 1 + ((x - 1) / y);
}
