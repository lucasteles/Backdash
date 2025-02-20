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

/// <summary>
/// Platform Info
/// </summary>
public static class Platform
{
    /// <summary>
    /// Current Endianness
    /// </summary>
    public static readonly Endianness Endianness =
        BitConverter.IsLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian;

    /// <summary>
    /// Get Endianness for Network if <paramref name="network"/> is True <see cref="Network.Endianness.BigEndian"/>
    /// </summary>
    public static Endianness GetEndianness(bool network) => network ? Endianness.BigEndian : Endianness;
}
