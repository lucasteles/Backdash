// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace

using System.Runtime.InteropServices;

namespace nGGPO.Inputs;

[StructLayout(LayoutKind.Sequential, Size = 2)]
public struct Axis
{
    public sbyte X;
    public sbyte Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct PadInput
{
    [Flags]
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