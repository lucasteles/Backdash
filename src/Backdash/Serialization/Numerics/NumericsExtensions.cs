using System.Numerics;

namespace Backdash.Serialization.Numerics;

/// <summary>
/// Serialization extensions for System
/// <see cref="System.Numerics"/>
/// </summary>
public static class NumericsExtensions
{
    #region BinaryBufferReader

    /// <summary>Reads single <see cref="Vector2"/> from buffer.</summary>
    public static Vector2 ReadVector2(in this BinaryBufferReader reader)
    {
        var x = reader.ReadFloat();
        var y = reader.ReadFloat();
        return new(x, y);
    }

    /// <inheritdoc cref="ReadVector2(in Backdash.Serialization.BinaryBufferReader)"/>
    public static void ReadVector2(in this BinaryBufferReader reader, ref Vector2 value)
    {
        value.X = reader.ReadFloat();
        value.Y = reader.ReadFloat();
    }

    /// <summary>Reads single <see cref="Vector3"/> from buffer.</summary>
    public static Vector3 ReadVector3(in this BinaryBufferReader reader)
    {
        var x = reader.ReadFloat();
        var y = reader.ReadFloat();
        var z = reader.ReadFloat();
        return new(x, y, z);
    }

    /// <inheritdoc cref="ReadVector3(in Backdash.Serialization.BinaryBufferReader)"/>
    public static void ReadVector3(in this BinaryBufferReader reader, ref Vector3 value)
    {
        value.X = reader.ReadFloat();
        value.Y = reader.ReadFloat();
        value.Z = reader.ReadFloat();
    }

    /// <summary>Reads single <see cref="Vector4"/> from buffer.</summary>
    public static Vector4 ReadVector4(in this BinaryBufferReader reader)
    {
        var x = reader.ReadFloat();
        var y = reader.ReadFloat();
        var z = reader.ReadFloat();
        var w = reader.ReadFloat();
        return new(x, y, z, w);
    }

    /// <inheritdoc cref="ReadVector4(in Backdash.Serialization.BinaryBufferReader)"/>
    public static void ReadVector4(in this BinaryBufferReader reader, ref Vector4 value)
    {
        value.X = reader.ReadFloat();
        value.Y = reader.ReadFloat();
        value.Z = reader.ReadFloat();
        value.W = reader.ReadFloat();
    }

    /// <summary>Reads single <see cref="Quaternion"/> from buffer.</summary>
    public static Quaternion ReadQuaternion(in this BinaryBufferReader reader)
    {
        var x = reader.ReadFloat();
        var y = reader.ReadFloat();
        var z = reader.ReadFloat();
        var w = reader.ReadFloat();
        return new(x, y, z, w);
    }

    /// <inheritdoc cref="ReadQuaternion(in Backdash.Serialization.BinaryBufferReader)"/>
    public static void ReadQuaternion(in this BinaryBufferReader reader, ref Quaternion value)
    {
        value.X = reader.ReadFloat();
        value.Y = reader.ReadFloat();
        value.Z = reader.ReadFloat();
        value.W = reader.ReadFloat();
    }

    /// <inheritdoc cref="ReadVector2(in Backdash.Serialization.BinaryBufferReader)"/>
    public static Vector2? ReadNullableVector2(in this BinaryBufferReader reader) =>
        reader.ReadBoolean() ? reader.ReadVector2() : null;

    /// <inheritdoc cref="ReadVector3(in Backdash.Serialization.BinaryBufferReader)"/>
    public static Vector3? ReadNullableVector3(in this BinaryBufferReader reader) =>
        reader.ReadBoolean() ? reader.ReadVector3() : null;

    /// <inheritdoc cref="ReadVector4(in Backdash.Serialization.BinaryBufferReader)"/>
    public static Vector4? ReadNullableVector4(in this BinaryBufferReader reader) =>
        reader.ReadBoolean() ? reader.ReadVector4() : null;

    /// <inheritdoc cref="ReadQuaternion(in Backdash.Serialization.BinaryBufferReader)"/>
    public static Quaternion? ReadNullableQuaternion(in this BinaryBufferReader reader) =>
        reader.ReadBoolean() ? reader.ReadQuaternion() : null;

    #endregion

    #region BinaryRawBufferWriter

    /// <summary>Writes single <see cref="Vector2"/> <paramref name="value"/> into buffer.</summary>
    public static void Write(in this BinaryRawBufferWriter writer, in Vector2 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
    }

    /// <summary>Writes single <see cref="Vector3"/> <paramref name="value"/> into buffer.</summary>
    public static void Write(in this BinaryRawBufferWriter writer, in Vector3 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }

    /// <summary>Writes single <see cref="Vector4"/> <paramref name="value"/> into buffer.</summary>
    public static void Write(in this BinaryRawBufferWriter writer, in Vector4 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
        writer.Write(value.W);
    }

    /// <summary>Writes single <see cref="Quaternion"/> <paramref name="value"/> into buffer.</summary>
    public static void Write(in this BinaryRawBufferWriter writer, in Quaternion value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
        writer.Write(value.W);
    }

    #endregion

    #region BinaryBufferWriter

    /// <summary>Writes single <see cref="Vector2"/> <paramref name="value"/> into buffer.</summary>
    public static void Write(in this BinaryBufferWriter writer, in Vector2 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
    }

    /// <summary>Writes single <see cref="Vector3"/> <paramref name="value"/> into buffer.</summary>
    public static void Write(in this BinaryBufferWriter writer, in Vector3 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }

    /// <summary>Writes single <see cref="Vector4"/> <paramref name="value"/> into buffer.</summary>
    public static void Write(in this BinaryBufferWriter writer, in Vector4 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
        writer.Write(value.W);
    }

    /// <summary>Writes single <see cref="Quaternion"/> <paramref name="value"/> into buffer.</summary>
    public static void Write(in this BinaryBufferWriter writer, in Quaternion value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
        writer.Write(value.W);
    }


    /// <inheritdoc cref="Write(in Backdash.Serialization.BinaryRawBufferWriter,in System.Numerics.Vector2)"/>
    public static void Write(in this BinaryBufferWriter writer, in Vector2? value)
    {
        writer.Write(value.HasValue);
        if (value.HasValue)
            writer.Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="Write(in Backdash.Serialization.BinaryRawBufferWriter,in System.Numerics.Vector3)"/>
    public static void Write(in this BinaryBufferWriter writer, in Vector3? value)
    {
        writer.Write(value.HasValue);
        if (value.HasValue)
            writer.Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="Write(in Backdash.Serialization.BinaryRawBufferWriter,in System.Numerics.Vector4)"/>
    public static void Write(in this BinaryBufferWriter writer, in Vector4? value)
    {
        writer.Write(value.HasValue);
        if (value.HasValue)
            writer.Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="Write(in Backdash.Serialization.BinaryRawBufferWriter,in System.Numerics.Quaternion)"/>
    public static void Write(in this BinaryBufferWriter writer, in Quaternion? value)
    {
        writer.Write(value.HasValue);
        if (value.HasValue)
            writer.Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    #endregion
}
