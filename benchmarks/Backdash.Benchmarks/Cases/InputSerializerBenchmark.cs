// ReSharper disable UnassignedField.Global; InconsistentNaming

#pragma warning disable S101

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Backdash.Network;
using Backdash.Serialization;
using Backdash.Serialization.Internal;
using Backdash.Synchronizing.Input.Confirmed;

namespace Backdash.Benchmarks.Cases;

[InProcess]
[GcForce, MemoryDiagnoser]
[RankColumn]
public class InputSerializerBenchmark
{
    [Params(1, 2, 3, 4)]
    public int Size;

    const int multiplier = 10_000_000;

    [GlobalSetup]
    public void Setup()
    {
        Random random = new(42);
        var values = Enum.GetValues<InputType>();
        var inputs = random.GetItems(values, Size);

        data = new(inputs);
        buffer = new byte[Unsafe.SizeOf<ConfirmedInputs<InputType>>()];
    }

    [IterationSetup]
    public void BeforeEach()
    {
        Array.Clear(buffer);
        restored = new();
    }

    [IterationCleanup]
    public void AfterEach() => System.Diagnostics.Trace.Assert(data == restored);

    ConfirmedInputs<InputType> data;
    byte[] buffer = [];
    ConfirmedInputs<InputType> restored = new();

    static readonly IBinarySerializer<InputType> sub = EnumBinarySerializer.Create<InputType>(Endianness.BigEndian);

    static readonly ConfirmedInputsSerializer<InputType> baseSerializer = new(sub);

    static readonly ConfirmedInputsSerializer<InputType> customSerializer = new(
        new InputTypeCustomSerializer(sub.Endianness));

    static readonly ConfirmedInputsSerializer<InputType> longSerializer = new(
        new EnumBinarySerializer<InputType, long>(new InputTypeLongSerializer(sub.Endianness)));

    [Benchmark]
    public void Integer() => TestSerialize(baseSerializer);

    [Benchmark]
    public void Long() => TestSerialize(longSerializer);

    [Benchmark]
    public void Custom() => TestSerialize(customSerializer);


    void TestSerialize(IBinarySerializer<ConfirmedInputs<InputType>> serializer)
    {
        for (int i = 0; i < multiplier; i++)
        {
            var writtenCount = serializer.Serialize(in data, buffer);
            var writtenSpan = buffer.AsSpan(0, writtenCount);
            serializer.Deserialize(writtenSpan, ref restored);
        }
    }
}

public enum InputType : long
{
    N0 = long.MinValue,
    N1 = 1,
    N2 = short.MaxValue,
    N3 = int.MaxValue,
    N4 = long.MaxValue,
}

sealed class InputTypeCustomSerializer(Endianness endianness) : IBinarySerializer<InputType>
{
    /// <inheritdoc/>
    public Endianness Endianness => endianness;

    public int Serialize(in InputType data, Span<byte> buffer)
    {
        switch (endianness)
        {
            case Endianness.LittleEndian:
                BinaryPrimitives.WriteInt64LittleEndian(buffer, (long)data);
                break;
            case Endianness.BigEndian:
                BinaryPrimitives.WriteInt64BigEndian(buffer, (long)data);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(endianness), endianness, null);
        }

        return sizeof(long);
    }

    public int Deserialize(ReadOnlySpan<byte> data, ref InputType value)
    {
        switch (endianness)
        {
            case Endianness.LittleEndian:
                value = (InputType)BinaryPrimitives.ReadInt64LittleEndian(data);
                break;
            case Endianness.BigEndian:
                value = (InputType)BinaryPrimitives.ReadInt64BigEndian(data);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(endianness), endianness, null);
        }

        return sizeof(long);
    }
}

sealed class InputTypeLongSerializer(Endianness endianness) : IBinarySerializer<long>
{
    /// <inheritdoc/>
    public Endianness Endianness => endianness;

    public int Serialize(in long data, Span<byte> buffer)
    {
        switch (endianness)
        {
            case Endianness.LittleEndian:
                BinaryPrimitives.WriteInt64LittleEndian(buffer, data);
                break;
            case Endianness.BigEndian:
                BinaryPrimitives.WriteInt64BigEndian(buffer, data);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(endianness), endianness, null);
        }

        return sizeof(long);
    }

    public int Deserialize(ReadOnlySpan<byte> data, ref long value)
    {
        switch (endianness)
        {
            case Endianness.LittleEndian:
                value = BinaryPrimitives.ReadInt64LittleEndian(data);
                break;
            case Endianness.BigEndian:
                value = BinaryPrimitives.ReadInt64BigEndian(data);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(endianness), endianness, null);
        }

        return sizeof(long);
    }
}

#pragma warning restore S101
