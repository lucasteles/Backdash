using System.Drawing;
using System.Runtime.InteropServices;

namespace Backdash.Tests.Utils.Types;

public enum IntEnum { A, B, C }

public enum UIntEnum : uint { A, B, C }

public enum ULongEnum : ulong { A, B, C }

public enum LongEnum : long { A, B, C }

public enum ShortEnum : short { A, B, C }

public enum UShortEnum : ushort { A, B, C }

public enum ByteEnum : byte { A, B, C }

public enum SByteEnum : sbyte { A, B, C }

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
