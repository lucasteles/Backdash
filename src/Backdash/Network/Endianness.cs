namespace Backdash.Network;

/// <summary>
/// Defines a endianness value
/// </summary>
public enum Endianness : byte
{
    /// <summary>Little endian byte order</summary>
    LittleEndian,

    /// <summary>Big endian byte order</summary>
    BigEndian,
}

static class Platform
{
    public static readonly Endianness Endianness =
        BitConverter.IsLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian;

    public static Endianness GetEndianness(bool network) => network ? Endianness.BigEndian : Endianness;
}
