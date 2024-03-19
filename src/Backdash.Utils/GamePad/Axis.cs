using System.Runtime.InteropServices;

namespace Backdash.GamePad;

[StructLayout(LayoutKind.Sequential, Size = 2), Serializable]
public record struct Axis(sbyte X, sbyte Y)
{
    public sbyte X = X;
    public sbyte Y = Y;
}
