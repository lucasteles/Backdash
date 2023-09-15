using System;
using System.Reflection;
using nGGPO.Serialization.Buffer;
using nGGPO.Utils;

namespace nGGPO.Serialization;

public sealed class StructMarshalBinarySerializer<T> : IBinarySerializer<T> where T : struct
{
    public PooledBuffer Serialize(T message) =>
        Mem.StructToBytes(message);

    public T Deserialize(byte[] body) =>
        Mem.BytesToStruct<T>(body);
}

static class PrimitiveBinarySerializers
{
    public class ByteSerializer : BinarySerializer<byte>
    {
        public override int SizeOf(in byte data) => sizeof(byte);

        protected internal override void Serialize(ref NetworkBufferWriter writer, in byte data) =>
            writer.Write(data);

        protected internal override byte Deserialize(ref NetworkBufferReader reader) =>
            reader.ReadByte();
    }

    public class ShortSerializer : BinarySerializer<short>
    {
        public override int SizeOf(in short data) => sizeof(short);

        protected internal override void Serialize(ref NetworkBufferWriter writer, in short data) =>
            writer.Write(data);

        protected internal override short Deserialize(ref NetworkBufferReader reader) =>
            reader.ReadShort();
    }

    public class IntSerializer : BinarySerializer<int>
    {
        public override int SizeOf(in int data) => sizeof(int);

        protected internal override void Serialize(ref NetworkBufferWriter writer, in int data) =>
            writer.Write(data);

        protected internal override int Deserialize(ref NetworkBufferReader reader) =>
            reader.ReadInt();
    }

    public class LongSerializer : BinarySerializer<long>
    {
        public override int SizeOf(in long data) => sizeof(long);

        protected internal override void Serialize(ref NetworkBufferWriter writer, in long data) =>
            writer.Write(data);

        protected internal override long Deserialize(ref NetworkBufferReader reader) =>
            reader.ReadLong();
    }

    public class SByteSerializer : BinarySerializer<sbyte>
    {
        public override int SizeOf(in sbyte data) => sizeof(sbyte);

        protected internal override void Serialize(ref NetworkBufferWriter writer, in sbyte data) =>
            writer.Write(data);

        protected internal override sbyte Deserialize(ref NetworkBufferReader reader) =>
            reader.ReadSByte();
    }

    public class UShortSerializer : BinarySerializer<ushort>
    {
        public override int SizeOf(in ushort data) => sizeof(ushort);

        protected internal override void
            Serialize(ref NetworkBufferWriter writer, in ushort data) =>
            writer.Write(data);

        protected internal override ushort Deserialize(ref NetworkBufferReader reader) =>
            reader.ReadUShort();
    }

    public class UIntSerializer : BinarySerializer<uint>
    {
        public override int SizeOf(in uint data) => sizeof(uint);

        protected internal override void Serialize(ref NetworkBufferWriter writer, in uint data) =>
            writer.Write(data);

        protected internal override uint Deserialize(ref NetworkBufferReader reader) =>
            reader.ReadUInt();
    }

    public class ULongSerializer : BinarySerializer<ulong>
    {
        public override int SizeOf(in ulong data) => sizeof(ulong);

        protected internal override void Serialize(ref NetworkBufferWriter writer, in ulong data) =>
            writer.Write(data);

        protected internal override ulong Deserialize(ref NetworkBufferReader reader) =>
            reader.ReadULong();
    }

    public class CharSerializer : BinarySerializer<char>
    {
        public override int SizeOf(in char data) => sizeof(char);

        protected internal override void Serialize(ref NetworkBufferWriter writer, in char data) =>
            writer.Write(data);

        protected internal override char Deserialize(ref NetworkBufferReader reader) =>
            reader.ReadChar();
    }

    public class BoolSerializer : BinarySerializer<bool>
    {
        public override int SizeOf(in bool data) => sizeof(bool);

        protected internal override void Serialize(ref NetworkBufferWriter writer, in bool data) =>
            writer.Write(data);

        protected internal override bool Deserialize(ref NetworkBufferReader reader) =>
            reader.ReadBool();
    }

    public class EnumSerializer<TEnum, TInt> : BinarySerializer<TEnum>
        where TEnum : unmanaged, Enum
        where TInt : unmanaged
    {
        readonly BinarySerializer<TInt> valueSerializer;

        public EnumSerializer(BinarySerializer<TInt> valueSerializer) =>
            this.valueSerializer = valueSerializer;

        public override int SizeOf(in TEnum data) => valueSerializer.SizeOf(default);

        protected internal override void Serialize(ref NetworkBufferWriter writer,
            in TEnum data) =>
            valueSerializer.Serialize(ref writer, Mem.EnumAsInteger<TEnum, TInt>(data));

        protected internal override TEnum Deserialize(ref NetworkBufferReader reader) =>
            Mem.IntegerAsEnum<TEnum, TInt>(valueSerializer.Deserialize(ref reader));
    }

    static object? GetPrimitiveSerializer(Type t)
    {
        if (!t.IsPrimitive) return null;

        if (t == typeof(char))
            return new CharSerializer();

        if (t == typeof(bool))
            return new BoolSerializer();

        if (t == typeof(byte))
            return new ByteSerializer();

        if (t == typeof(short))
            return new ShortSerializer();

        if (t == typeof(int))
            return new IntSerializer();

        if (t == typeof(long))
            return new LongSerializer();

        if (t == typeof(sbyte))
            return new SByteSerializer();

        if (t == typeof(ushort))
            return new UShortSerializer();

        if (t == typeof(uint))
            return new UIntSerializer();

        if (t == typeof(ulong))
            return new ULongSerializer();

        return null;
    }

    static IBinarySerializer<TEnum>? GetEnumSerializer<TEnum>() where TEnum : unmanaged, Enum
    {
        var enumType = typeof(TEnum);

        if (!enumType.IsEnum) return null;
        var t = Enum.GetUnderlyingType(enumType);

        if (t == typeof(byte))
            return new EnumSerializer<TEnum, byte>(new ByteSerializer());

        if (t == typeof(short))
            return new EnumSerializer<TEnum, short>(new ShortSerializer());

        if (t == typeof(int))
            return new EnumSerializer<TEnum, int>(new IntSerializer());

        if (t == typeof(long))
            return new EnumSerializer<TEnum, long>(new LongSerializer());

        if (t == typeof(sbyte))
            return new EnumSerializer<TEnum, sbyte>(new SByteSerializer());

        if (t == typeof(ushort))
            return new EnumSerializer<TEnum, ushort>(new UShortSerializer());

        if (t == typeof(uint))
            return new EnumSerializer<TEnum, uint>(new UIntSerializer());

        if (t == typeof(ulong))
            return new EnumSerializer<TEnum, ulong>(new ULongSerializer());

        return null;
    }

    public static IBinarySerializer<T>? GetSerializer<T>() where T : unmanaged
    {
        var t = typeof(T);

        if (t.IsEnum)
            return typeof(PrimitiveBinarySerializers)
                    .GetMethod(nameof(GetEnumSerializer),
                        BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(t)
                    .Invoke(null, Array.Empty<object>())
                as IBinarySerializer<T>;

        return GetPrimitiveSerializer(t) as IBinarySerializer<T>;
    }
}

public static class BinarySerializers
{
    public static IBinarySerializer<TInput> Get<TInput>() where TInput : struct
    {
        var inputType = typeof(TInput);

        if (inputType is {IsEnum: false, IsPrimitive: false, StructLayoutAttribute: not null})
            return new StructMarshalBinarySerializer<TInput>();

        if (typeof(PrimitiveBinarySerializers)
                .GetMethod(nameof(PrimitiveBinarySerializers.GetSerializer),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(inputType)
                .Invoke(null, Array.Empty<object>()) is IBinarySerializer<TInput>
            primitiveSerializer
           )
            return primitiveSerializer;

        throw new InvalidOperationException(
            $"Unable to infer serializer for type {typeof(TInput).FullName}");
    }
}