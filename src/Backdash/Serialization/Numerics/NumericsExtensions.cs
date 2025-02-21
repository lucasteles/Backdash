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

    /// <summary>Reads single <see cref="Vector3"/> from buffer.</summary>
    public static Vector3 ReadVector3(in this BinaryBufferReader reader)
    {
        var x = reader.ReadFloat();
        var y = reader.ReadFloat();
        var z = reader.ReadFloat();
        return new(x, y, z);
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

    /// <summary>Reads single <see cref="Quaternion"/> from buffer.</summary>
    public static Quaternion ReadQuaternion(in this BinaryBufferReader reader)
    {
        var x = reader.ReadFloat();
        var y = reader.ReadFloat();
        var z = reader.ReadFloat();
        var w = reader.ReadFloat();
        return new(x, y, z, w);
    }

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

    #endregion
}
