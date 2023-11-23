using System;

namespace nGGPO.Input;

readonly record struct Frame : IComparable<Frame>, IComparable<int>, IEquatable<int>
{
    public const sbyte NullValue = -1;
    public static readonly Frame Null = new(NullValue);
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