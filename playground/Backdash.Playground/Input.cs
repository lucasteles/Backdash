using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backdash.Playground;

[InlineArray(10)]
[DebuggerDisplay("Buffer {ToString()}")]
public struct ValueBuffer
{
    byte element0;

    public readonly override string ToString()
    {
        ReadOnlySpan<byte> bytes = this;
        return $"[{string.Join(", ", bytes.ToArray().Select(x => (int)x))}]";
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Input
{
    public int S;
    public byte A;
    public uint B;

    [JsonIgnore]
    public ValueBuffer Bits;

    // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    // public byte[] Bits; /* must be last */

    public override string ToString() =>
        $"{JsonSerializer.Serialize(this, new JsonSerializerOptions { IncludeFields = true })}; Buffer: {Bits}";
}