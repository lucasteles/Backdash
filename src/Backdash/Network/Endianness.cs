namespace Backdash.Network;

/// <summary>
///     Defines a endianness value
/// </summary>
public enum Endianness : byte
{
    /// <summary>Little endian byte order</summary>
    LittleEndian,

    /// <summary>Big endian byte order</summary>
    BigEndian,
}

/// <summary>
///     Platform Info
/// </summary>
public static class Platform
{
    /// <summary>
    ///     Current Endianness
    /// </summary>
    public static readonly Endianness Endianness =
        BitConverter.IsLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian;

    /// <summary>
    ///     Get Endianness for Network.
    ///     If <paramref name="network" /> is True, returns <see cref="Network.Endianness.BigEndian" />.
    ///     Otherwise, returns the current platform endianness, same as <see cref="Platform" />.
    ///     <see cref="Platform.Endianness" />
    /// </summary>
    public static Endianness GetNetworkEndianness(bool network) => network ? Endianness.BigEndian : Endianness;
}
