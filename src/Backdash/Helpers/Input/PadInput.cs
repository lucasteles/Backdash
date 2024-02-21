using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming
namespace Backdash.Helpers.Input;

[StructLayout(LayoutKind.Sequential, Size = 2), Serializable]
public record struct Axis
{
    public sbyte X;
    public sbyte Y;
}

[StructLayout(LayoutKind.Sequential), Serializable]
public record struct PadInput
{
    [Flags, Serializable]
    public enum PadButtons : short
    {
        None = 0,
        Select = 1 << 0,
        Up = 1 << 1,
        Down = 1 << 2,
        Left = 1 << 3,
        Right = 1 << 4,
        X = 1 << 5,
        Y = 1 << 6,
        A = 1 << 7,
        B = 1 << 8,
        LB = 1 << 9,
        RB = 1 << 10,
        LSB = 1 << 11,
        RSB = 1 << 12,
    }

    public PadButtons Buttons;
    public byte LT;
    public byte RT;
    public Axis LeftAxis;
    public Axis RightAxis;
}
