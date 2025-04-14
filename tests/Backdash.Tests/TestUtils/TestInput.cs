using System.Runtime.CompilerServices;
using Backdash.Core;
using Backdash.Network;
using Backdash.Serialization;

namespace Backdash.Tests.TestUtils;

[InlineArray(TestInput.Capacity), Serializable]
public struct TestInputBuffer
{
    byte element0;
    public TestInputBuffer(ReadOnlySpan<byte> bits) => bits.CopyTo(this);

    public readonly string ToString(bool trimZeros) =>
        Mem.GetBitString(this, trimRightZeros: trimZeros);

    public override readonly string ToString() => ToString(trimZeros: true);

    ///<inheritdoc/>
    public override readonly int GetHashCode() => Mem.GetHashCode<byte>(this);

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="other">The <see cref="TestInputBuffer"/> to compare with the current object.</param>
    /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
    public readonly bool Equals(TestInputBuffer other) => this[..].SequenceEqual(other);

    ///<inheritdoc/>
    public override readonly bool Equals(object? obj) => obj is TestInputBuffer other && Equals(other);
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
    public Endianness Endianness { get; } = Platform.GetNetworkEndianness(false);

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
