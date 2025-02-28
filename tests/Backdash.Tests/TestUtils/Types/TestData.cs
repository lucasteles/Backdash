using System.Drawing;
using System.Runtime.InteropServices;
using Backdash.Serialization;

// ReSharper disable UnusedMember.Global, NotAccessedField.Global, EnumUnderlyingTypeIsInt
#pragma warning disable S2344, S1939, S2346
namespace Backdash.Tests.TestUtils.Types;

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

public record SimpleRefData : IBinarySerializable
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

    public void Serialize(ref readonly BinaryBufferWriter writer)
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

[Flags]
public enum ByteEnum : byte
{
    None = 0b00000000,
    A1 = 0b00000001,
    A2 = 0b00000010,
    A3 = 0b00000100,
    A4 = 0b00001000,
    A5 = 0b00010000,
    A6 = 0b00100000,
    A7 = 0b01000000,
    A8 = 0b10000000,
}

[Flags]
public enum SByteEnum : sbyte
{
    None = 0b00000000,
    A1 = 0b00000001,
    A2 = 0b00000010,
    A3 = 0b00000100,
    A4 = 0b00001000,
    A5 = 0b00010000,
    A6 = 0b00100000,
    A7 = 0b01000000,
}

[Flags]
public enum UShortEnum : ushort
{
    None = 0,

    A1 = 1 << 0,
    A2 = 1 << 1,
    A3 = 1 << 2,
    A4 = 1 << 3,
    A5 = 1 << 4,
    A6 = 1 << 5,
    A7 = 1 << 6,
    A8 = 1 << 7,

    B1 = 1 << 8,
    B2 = 1 << 9,
    B3 = 1 << 10,
    B4 = 1 << 11,
    B5 = 1 << 12,
    B6 = 1 << 13,
    B7 = 1 << 14,
    B8 = 1 << 15,
}

[Flags]
public enum ShortEnum : short
{
    None = 0,

    A1 = 1 << 0,
    A2 = 1 << 1,
    A3 = 1 << 2,
    A4 = 1 << 3,
    A5 = 1 << 4,
    A6 = 1 << 5,
    A7 = 1 << 6,
    A8 = 1 << 7,

    B1 = 1 << 8,
    B2 = 1 << 9,
    B3 = 1 << 10,
    B4 = 1 << 11,
    B5 = 1 << 12,
    B6 = 1 << 13,
    B7 = 1 << 14,
}

[Flags]
public enum UIntEnum : uint
{
    None = 0,
    A1 = 1u << 0,
    A2 = 1u << 1,
    A3 = 1u << 2,
    A4 = 1u << 3,
    A5 = 1u << 4,
    A6 = 1u << 5,
    A7 = 1u << 6,
    A8 = 1u << 7,

    B1 = 1u << 8,
    B2 = 1u << 9,
    B3 = 1u << 10,
    B4 = 1u << 11,
    B5 = 1u << 12,
    B6 = 1u << 13,
    B7 = 1u << 14,
    B8 = 1u << 15,

    C1 = 1u << 16,
    C2 = 1u << 17,
    C3 = 1u << 18,
    C4 = 1u << 19,
    C5 = 1u << 20,
    C6 = 1u << 21,
    C7 = 1u << 22,
    C8 = 1u << 23,

    D1 = 1u << 24,
    D2 = 1u << 25,
    D3 = 1u << 26,
    D4 = 1u << 27,
    D5 = 1u << 28,
    D6 = 1u << 29,
    D7 = 1u << 30,
    D8 = 1u << 31,
}

[Flags]
public enum IntEnum : int
{
    None = 0,
    A1 = 1 << 0,
    A2 = 1 << 1,
    A3 = 1 << 2,
    A4 = 1 << 3,
    A5 = 1 << 4,
    A6 = 1 << 5,
    A7 = 1 << 6,
    A8 = 1 << 7,

    B1 = 1 << 8,
    B2 = 1 << 9,
    B3 = 1 << 10,
    B4 = 1 << 11,
    B5 = 1 << 12,
    B6 = 1 << 13,
    B7 = 1 << 14,
    B8 = 1 << 15,

    C1 = 1 << 16,
    C2 = 1 << 17,
    C3 = 1 << 18,
    C4 = 1 << 19,
    C5 = 1 << 20,
    C6 = 1 << 21,
    C7 = 1 << 22,
    C8 = 1 << 23,

    D1 = 1 << 24,
    D2 = 1 << 25,
    D3 = 1 << 26,
    D4 = 1 << 27,
    D5 = 1 << 28,
    D6 = 1 << 29,
    D7 = 1 << 30,
    D8 = 1 << 31,
}

[Flags]
public enum ULongEnum : ulong
{
    None = 0,
    A1 = 1ul << 0,
    A2 = 1ul << 1,
    A3 = 1ul << 2,
    A4 = 1ul << 3,
    A5 = 1ul << 4,
    A6 = 1ul << 5,
    A7 = 1ul << 6,
    A8 = 1ul << 7,

    B1 = 1ul << 8,
    B2 = 1ul << 9,
    B3 = 1ul << 10,
    B4 = 1ul << 11,
    B5 = 1ul << 12,
    B6 = 1ul << 13,
    B7 = 1ul << 14,
    B8 = 1ul << 15,

    C1 = 1ul << 16,
    C2 = 1ul << 17,
    C3 = 1ul << 18,
    C4 = 1ul << 19,
    C5 = 1ul << 20,
    C6 = 1ul << 21,
    C7 = 1ul << 22,
    C8 = 1ul << 23,

    D1 = 1ul << 24,
    D2 = 1ul << 25,
    D3 = 1ul << 26,
    D4 = 1ul << 27,
    D5 = 1ul << 28,
    D6 = 1ul << 29,
    D7 = 1ul << 30,
    D8 = 1ul << 31,

    E1 = 1ul << 32,
    E2 = 1ul << 33,
    E3 = 1ul << 34,
    E4 = 1ul << 35,
    E5 = 1ul << 36,
    E6 = 1ul << 37,
    E7 = 1ul << 38,
    E8 = 1ul << 39,

    F1 = 1ul << 40,
    F2 = 1ul << 41,
    F3 = 1ul << 42,
    F4 = 1ul << 43,
    F5 = 1ul << 44,
    F6 = 1ul << 45,
    F7 = 1ul << 46,
    F8 = 1ul << 47,

    G1 = 1ul << 48,
    G2 = 1ul << 49,
    G3 = 1ul << 50,
    G4 = 1ul << 51,
    G5 = 1ul << 52,
    G6 = 1ul << 53,
    G7 = 1ul << 54,
    G8 = 1ul << 55,

    H1 = 1ul << 56,
    H2 = 1ul << 57,
    H3 = 1ul << 58,
    H4 = 1ul << 59,
    H5 = 1ul << 60,
    H6 = 1ul << 61,
    H7 = 1ul << 62,
    H8 = 1ul << 63,
}

[Flags]
public enum LongEnum : long
{
    None = 0,
    A1 = 1L << 0,
    B1 = 1L << 1,
    C1 = 1L << 2,
    D1 = 1L << 3,
    E1 = 1L << 4,
    F1 = 1L << 5,
    G1 = 1L << 6,
    H1 = 1L << 7,

    I1 = 1L << 8,
    J1 = 1L << 9,
    K1 = 1L << 10,
    L1 = 1L << 11,
    M1 = 1L << 12,
    N1 = 1L << 13,
    O1 = 1L << 14,
    P1 = 1L << 15,

    Q1 = 1L << 16,
    S1 = 1L << 17,
    T1 = 1L << 18,
    U1 = 1L << 19,
    V1 = 1L << 20,
    W1 = 1L << 21,
    X1 = 1L << 22,
    Y1 = 1L << 23,

    Z1 = 1L << 24,
    A2 = 1L << 25,
    B2 = 1L << 26,
    C2 = 1L << 27,
    D2 = 1L << 28,
    E2 = 1L << 29,
    F2 = 1L << 30,
    G2 = 1L << 31,

    H2 = 1L << 32,
    I2 = 1L << 33,
    J2 = 1L << 34,
    K2 = 1L << 35,
    L2 = 1L << 36,
    M2 = 1L << 37,
    N2 = 1L << 38,
    O2 = 1L << 39,
}
