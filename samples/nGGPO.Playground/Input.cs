using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

[InlineArray(10)]
[DebuggerDisplay("Buffer {ToString()}")]
public struct ValueBuffer
{
#pragma warning disable CS0169 // Field is never used
    byte element0;
#pragma warning restore CS0169 // Field is never used

    public override string ToString()
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
