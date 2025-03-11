// ReSharper disable UnassignedField.Global, NonReadonlyMemberInGetHashCode

#pragma warning disable S2328, S4035

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Serialization;
using MemoryPack;

namespace Backdash.Benchmarks.Cases;

[RPlotExporter]
[InProcess, MemoryDiagnoser, RankColumn]
public class SerializationBenchmark
{
    TestData data = null!;
    TestData result = null!;

    readonly ArrayBufferWriter<byte> buffer = new((int)ByteSize.FromMebiBytes(10).ByteCount);

    Utf8JsonWriter jsonWriter = null!;

    readonly JsonSerializerOptions jsonOptions = new()
    {
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        WriteIndented = false,
        IncludeFields = true,
    };

    static TestData NewTestData(Random random)
    {
        TestData testData = new()
        {
            Field1 = random.NextBool(),
            Field2 = random.Next<ulong>(),
        };

        for (int i = 0; i < testData.Field3.Length; i++)
        {
            ref var entry = ref testData.Field3[i];
            entry.Field1 = random.Next();
            entry.Field2 = random.Next<uint>();
            entry.Field3 = random.Next<ulong>();
            entry.Field4 = random.Next<long>();
            entry.Field5 = random.Next<short>();
            entry.Field6 = random.Next<ushort>();
            entry.Field7 = random.Next<byte>();
            entry.Field8 = random.Next<sbyte>();
            random.Next(entry.Field9.AsSpan());
        }

        return testData;
    }

    [GlobalSetup]
    public void Setup()
    {
        Random random = new(42);
        data = NewTestData(random);
        jsonWriter = new(buffer);
    }

    [IterationSetup]
    public void BeforeEach()
    {
        buffer.Clear();
        jsonWriter.Reset();
        result = new();
    }

    [Benchmark]
    public void Backdash()
    {
        var writer = new BinaryBufferWriter(buffer, Endianness.LittleEndian);
        writer.Write(data);
        int offset = 0;
        var reader = new BinaryBufferReader(buffer.WrittenSpan, ref offset, Endianness.LittleEndian);
        reader.Read(result);
        Debug.Assert(data == result);
    }

    [Benchmark]
    public void MemoryPack()
    {
        MemoryPackSerializer.Serialize(buffer, data);
        MemoryPackSerializer.Deserialize(buffer.WrittenSpan, ref result!);
        Debug.Assert(data == result);
    }

    [Benchmark]
    public void SystemJson()
    {
        JsonSerializer.Serialize(jsonWriter, data, jsonOptions);
        Utf8JsonReader reader = new(buffer.WrittenSpan);
        result = JsonSerializer.Deserialize<TestData>(ref reader, jsonOptions)!;
        Debug.Assert(data == result);
    }
}

[MemoryPackable]
public sealed partial class TestData : IBinarySerializable, IEquatable<TestData>
{
    public bool Field1;
    public ulong Field2;
    public TestEntryData[] Field3;

    public TestData()
    {
        Field3 = new TestEntryData[1_000];
        for (var i = 0; i < Field3.Length; i++)
            Field3[i].Field9 = new int[10_000];
    }

    public void Serialize(ref readonly BinaryBufferWriter writer)
    {
        writer.Write(in Field1);
        writer.Write(in Field2);
        writer.Write(in Field3);
    }

    public void Deserialize(ref readonly BinaryBufferReader reader)
    {
        reader.Read(ref Field1);
        reader.Read(ref Field2);
        reader.Read(in Field3);
    }

    public override int GetHashCode() => throw new InvalidOperationException();

    public bool Equals(TestData? other) => Equals(this, other);
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is TestData other && Equals(other));

    public static bool Equals(TestData? left, TestData? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;

        return left.Field1 == right.Field1
               && left.Field2 == right.Field2
               && left.Field3.AsSpan().SequenceEqual(right.Field3);
    }

    public static bool operator ==(TestData? left, TestData? right) => Equals(left, right);
    public static bool operator !=(TestData? left, TestData? right) => !Equals(left, right);
}

[MemoryPackable]
public partial struct TestEntryData() : IBinarySerializable, IEquatable<TestEntryData>
{
    public int Field1;
    public uint Field2;
    public ulong Field3;
    public long Field4;
    public short Field5;
    public ushort Field6;
    public byte Field7;
    public sbyte Field8;
    public int[] Field9 = [];

    public readonly void Serialize(ref readonly BinaryBufferWriter writer)
    {
        writer.Write(in Field1);
        writer.Write(in Field2);
        writer.Write(in Field3);
        writer.Write(in Field4);
        writer.Write(in Field5);
        writer.Write(in Field6);
        writer.Write(in Field7);
        writer.Write(in Field8);
        writer.Write(Field9);
    }

    public void Deserialize(ref readonly BinaryBufferReader reader)
    {
        Field1 = reader.ReadInt32();
        Field2 = reader.ReadUInt32();
        Field3 = reader.ReadUInt64();
        Field4 = reader.ReadInt64();
        Field5 = reader.ReadInt16();
        Field6 = reader.ReadUInt16();
        Field7 = reader.ReadByte();
        Field8 = reader.ReadSByte();
        reader.Read(Field9);
    }

    public override readonly int GetHashCode() => throw new InvalidOperationException();

    public readonly bool Equals(TestEntryData other) => Equals(in this, in other);
    public override readonly bool Equals(object? obj) => obj is TestEntryData other && Equals(in this, in other);

    public static bool Equals(in TestEntryData left, in TestEntryData right) =>
        left.Field1 == right.Field1 &&
        left.Field2 == right.Field2 &&
        left.Field3 == right.Field3 &&
        left.Field4 == right.Field4 &&
        left.Field5 == right.Field5 &&
        left.Field6 == right.Field6 &&
        left.Field7 == right.Field7 &&
        left.Field8 == right.Field8 &&
        left.Field9.AsSpan().SequenceEqual(right.Field9);

    public static bool operator ==(TestEntryData left, TestEntryData right) => Equals(in left, in right);
    public static bool operator !=(TestEntryData left, TestEntryData right) => !Equals(in left, in right);
}

public static class Extensions
{
    public static T Next<T>(this Random random) where T : unmanaged
    {
        var result = new T();
        var bytes = Mem.AsBytes(ref result);
        random.NextBytes(bytes);
        return result;
    }

    public static void Next<T>(this Random random, Span<T> buffer) where T : unmanaged =>
        random.NextBytes(MemoryMarshal.AsBytes(buffer));

    public static bool NextBool(this Random random) => random.Next(0, 2) == 1;
}
