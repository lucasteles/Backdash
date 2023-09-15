using System;
using System.Diagnostics;
using System.Text;
using nGGPO.DataStructure;
using nGGPO.Utils;

namespace nGGPO.Input;

readonly record struct Frame : IComparable<Frame>, IComparable<int>, IEquatable<int>
{
    public const sbyte NullValue = -1;
    public static readonly Frame Null = new();
    public static readonly Frame Zero = new(0);
    public int Number { get; } = NullValue;
    public Frame(int number) => Number = number;
    public Frame Next => new(Number + 1);
    public bool IsNull => Number is NullValue;
    public bool IsValid => !IsNull;

    public int CompareTo(Frame other) => Number.CompareTo(other.Number);
    public int CompareTo(int other) => Number.CompareTo(other);
    public bool Equals(int other) => Number == other;

    public override string ToString() => Number.ToString();

    public static Frame operator +(Frame a, Frame b) => new(a.Number + b.Number);
    public static Frame operator +(Frame a, int b) => new(a.Number + b);
    public static Frame operator +(int a, Frame b) => new(a + b.Number);
    public static Frame operator ++(Frame frame) => frame.Next;

    public static implicit operator int(Frame frame) => frame.Number;
    public static explicit operator Frame(int frame) => new(frame);
}

struct GameInput : IEquatable<GameInput>, IDisposable
{
    readonly MemoryBuffer<byte> buffer;
    public const int MaxBytes = 8;

    public Frame Frame { get; private set; } = Frame.Null;
    public int Size { get; }
    public BitVector Bits { get; }
    public static GameInput Empty => new();

    public GameInput(int size)
    {
        Size = size;
        Bits = BitVector.Empty;
        buffer = MemoryBuffer<byte>.Empty;
    }

    public GameInput(MemoryBuffer<byte> ibits, int size)
    {
        Trace.Assert(ibits.Length <= MaxBytes * Max.Players);
        Trace.Assert(ibits.Length > 0);

        Size = size;
        buffer = ibits;
        Bits = new(ibits.Memory);
    }

    public GameInput(MemoryBuffer<byte> ibits) : this(ibits, ibits.Length)
    {
    }

    public bool IsEmpty => Size is 0 && Bits.IsEmpty;
    public void IncrementFrame() => Frame = Frame.Next;
    public void SetFrame(Frame frame) => Frame = frame;
    public void ResetFrame() => Frame = Frame.Null;
    public void Clear() => Bits.Erase();

    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.AppendFormat("{{ Frame: {0},", Frame);
        builder.AppendFormat(" Size: {0}, Input: ", Size);

        builder.Append(Bits.ToString(splitAt: Max.Players));

        builder.Append(" }");

        return builder.ToString();
    }

    public bool Equals(GameInput other, bool bitsOnly)
    {
        if (!bitsOnly && Frame != other.Frame)
            Logger.Info("frames don't match: {}, {}", Frame, other.Frame);

        if (Size != other.Size)
            Logger.Info("sizes don't match: {}, {}", Size, other.Size);

        if (Bits.Equals(other.Bits))
            Logger.Info("bits don't match");

        Trace.Assert(Size > 0 && other.Size > 0);

        return (bitsOnly || Frame == other.Frame)
               && Size == other.Size
               && Bits.Equals(other.Bits);
    }

    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => HashCode.Combine(Size, Bits, Frame);
    public bool Equals(GameInput other) => Equals(other, false);
    public override bool Equals(object? obj) => obj is GameInput gi && Equals(gi);
    public static bool operator ==(GameInput a, GameInput b) => a.Equals(b);
    public static bool operator !=(GameInput a, GameInput b) => !(a == b);

    public void Dispose() => buffer.Dispose();
}