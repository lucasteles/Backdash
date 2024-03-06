using System.Runtime.CompilerServices;
using Backdash.Core;
using Backdash.Serialization;

namespace Backdash.Tests.Utils;

[InlineArray(TestInput.Capacity), Serializable]
public struct TestInputBuffer
{
    byte element0;

    public TestInputBuffer(ReadOnlySpan<byte> bits) => bits.CopyTo(this);

    public readonly string ToString(bool trimZeros) =>
        Mem.GetBitString(this, trimRightZeros: trimZeros);

    public override readonly string ToString() => ToString(trimZeros: true);
}

[Serializable]
public record struct TestInput
{
    public const int Capacity = 4;

    public TestInputBuffer Buffer;
    public int Length;

    public TestInput(int length)
    {
        Length = length;
        Span<byte> localBuffer = stackalloc byte[length];
        Buffer = new(localBuffer);
    }

    public TestInput(ReadOnlySpan<byte> buffer)
    {
        Buffer = new(buffer);
        Length = buffer.Length;
    }

    public TestInput() : this(Capacity) { }

    public readonly void CopyTo(Span<byte> bytes) => Buffer[..Length].CopyTo(bytes);
    public override readonly string ToString() => Buffer.ToString();
}

sealed class TestInputSerializer : IBinarySerializer<TestInput>
{
    public int Serialize(in TestInput data, Span<byte> buffer)
    {
        data.CopyTo(buffer);
        return data.Length;
    }

    public int Deserialize(ReadOnlySpan<byte> data, ref TestInput value)
    {
        value = new(data);
        return data.Length;
    }
}
