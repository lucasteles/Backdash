using System.Runtime.InteropServices;

namespace Backdash.GamePad;

[StructLayout(LayoutKind.Sequential, Size = 2), Serializable]
public record struct Axis
{
    public sbyte X;
    public sbyte Y;
}
