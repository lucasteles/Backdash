using System.Drawing;
using System.Runtime.InteropServices;

// ReSharper disable UnusedMember.Global, NotAccessedField.Global, EnumUnderlyingTypeIsInt
#pragma warning disable S2344, S1939
namespace Backdash.Tests.TestUtils.Types;

public enum IntEnum : int
{
    A = int.MinValue, B, C, D, E, F, G, H, I, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z = int.MaxValue,
}

public enum UIntEnum : uint
{
    A = uint.MinValue, B, C, D, E, F, G, H, I, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z = uint.MaxValue,
}

public enum ULongEnum : ulong
{
    A = ulong.MinValue, B, C, D, E, F, G, H, I, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z = ulong.MaxValue,
}

public enum LongEnum : long
{
    A = long.MinValue, B, C, D, E, F, G, H, I, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z = long.MaxValue,
}

public enum ShortEnum : short
{
    A = short.MinValue, B, C, D, E, F, G, H, I, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z = short.MaxValue,
}

public enum UShortEnum : ushort
{
    A = ushort.MinValue, B, C, D, E, F, G, H, I, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z = ushort.MaxValue,
}

public enum ByteEnum : byte
{
    Zero = byte.MinValue,
    Two = 1 << 1,
    Four = 1 << 2,
    Eight = 1 << 3,
    Sixteen = 1 << 4,
    ThirtyTwo = 1 << 5,
    SixtyFour = 1 << 6,
    OneHundredTwentyEight = 1 << 7,
    TwoHundredFiftyFive = byte.MaxValue,
}

public enum SByteEnum : sbyte
{
    MinusOneHundredTwentyEight = sbyte.MinValue,
    MinusSixtyFour = -1 << 6,
    MinusThirtyTwo = -1 << 5,
    MinusSixteen = -1 << 4,
    MinusEight = -1 << 3,
    MinusFour = -1 << 2,
    MinusTwo = -1 << 1,
    Zero = 0,
    Two = 1 << 1,
    Four = 1 << 2,
    Eight = 1 << 3,
    Sixteen = 1 << 4,
    ThirtyTwo = 1 << 5,
    SixtyFour = 1 << 6,
    OneHundredTwentySeven = sbyte.MaxValue,
}

public record struct SimpleStructData
{
    public int Field1;
    public uint Field2;
    public ulong Field3;
    public long Field4;
    public short Field5;
    public ushort Field6;
    public byte Field7;
    public sbyte Field8;
    public Point Field9;
}

[StructLayout(LayoutKind.Sequential)]
public struct MarshalStructData
{
    public int Field1;
    public long Field2;
    public byte Field3;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    public byte[] FieldArray;

    public readonly bool IsEquivalent(MarshalStructData other) =>
        Field1 == other.Field1
        && Field2 == other.Field2
        && Field3 == other.Field3
        && FieldArray.SequenceEqual(other.FieldArray);
}
