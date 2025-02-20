using System.Drawing;
using System.Runtime.InteropServices;
using Backdash.Serialization;

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

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public record struct SimpleStructData : IBinarySerializable
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

    public readonly void Serialize(ref readonly BinaryBufferWriter writer)
    {
        writer.Write(in Field1);
        writer.Write(in Field2);
        writer.Write(in Field3);
        writer.Write(in Field4);
        writer.Write(in Field5);
        writer.Write(in Field6);
        writer.Write(in Field7);
        writer.Write(in Field8);
        writer.WriteStruct(in Field9);
    }

    public void Deserialize(ref readonly BinaryBufferReader reader)
    {
        Field1 = reader.ReadInt32();
        Field2 = reader.ReadUInt32();
        Field3 = reader.ReadUInt64();
        Field4 = reader.ReadInt64();
        Field5 = reader.ReadInt16();
        Field6 = reader.ReadUInt16();
        Field7 = reader.ReadByte();
        Field8 = reader.ReadSByte();
        Field9 = reader.ReadStruct<Point>();
    }
}
