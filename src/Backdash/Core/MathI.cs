using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Backdash;

/// <summary>
///     Int Math
/// </summary>
public static class MathI
{
    /// <summary>
    ///     Divide two integers ceiling the result
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CeilDiv(int x, int y) => x is 0 ? 0 : 1 + ((x - 1) / y);

    /// <summary>
    ///     Returns the sum of a span of <see cref="IBinaryInteger{TSelf}"/>.
    ///     Use SIMD if available.
    /// </summary>
    public static T Sum<T>(in ReadOnlySpan<T> span)
        where T : unmanaged, IBinaryInteger<T>, IAdditionOperators<T, T, T>
    {
        unchecked
        {
            var sum = T.Zero;
            ref var current = ref MemoryMarshal.GetReference(span);
            ref var limit = ref Unsafe.Add(ref current, span.Length);

            if (Vector.IsHardwareAccelerated && span.Length >= Vector<T>.Count)
            {
                var vecSize = Vector<T>.Count;
                var sumVec = Vector<T>.Zero;
                ref var vecLimit = ref Unsafe.Add(ref current, span.Length - vecSize);

                while (Unsafe.IsAddressLessThan(ref current, ref vecLimit))
                {
                    sumVec += new Vector<T>(MemoryMarshal.CreateSpan(ref current, vecSize));
                    current = ref Unsafe.Add(ref current, vecSize);
                }

                for (var i = 0; i < vecSize; i++)
                    sum += sumVec[i];
            }

            while (Unsafe.IsAddressLessThan(ref current, ref limit))
            {
                sum += current;
                current = ref Unsafe.Add(ref current, 1);
            }

            return sum;
        }
    }

    /// <inheritdoc cref="Sum{T}(in ReadOnlySpan{T})"/>
    public static T Sum<T>(in T[] values)
        where T : unmanaged, IBinaryInteger<T>, IAdditionOperators<T, T, T> => Sum((ReadOnlySpan<T>)values);

    /// <summary>
    ///     Returns the sum of a span of <see cref="IBinaryInteger{TSelf}"/>
    /// </summary>
    public static T SumRaw<T>(in ReadOnlySpan<T> span)
        where T : unmanaged, IBinaryInteger<T>, IAdditionOperators<T, T, T>
    {
        unchecked
        {
            var sum = T.Zero;
            ref var current = ref MemoryMarshal.GetReference(span);
            ref var limit = ref Unsafe.Add(ref current, span.Length);

            while (Unsafe.IsAddressLessThan(ref current, ref limit))
            {
                sum += current;
                current = ref Unsafe.Add(ref current, 1);
            }

            return sum;
        }
    }

    /// <inheritdoc cref="SumRaw{T}(in ReadOnlySpan{T})"/>
    public static T SumRaw<T>(in T[] values)
        where T : unmanaged, IBinaryInteger<T>, IAdditionOperators<T, T, T> => Sum((ReadOnlySpan<T>)values);

    /// <summary>
    ///     Returns the average sum of a span of <see cref="int"/>
    /// </summary>
    public static double Avg(in ReadOnlySpan<int> span)
    {
        if (span.IsEmpty) return 0;
        return Sum(in span) / (double)span.Length;
    }

    /// <inheritdoc cref="Avg(in ReadOnlySpan{int})"/>
    public static double Avg(in int[] values) => Avg((ReadOnlySpan<int>)values);
}
