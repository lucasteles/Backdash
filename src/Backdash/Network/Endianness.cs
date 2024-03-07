namespace Backdash.Network;
public enum Endianness : byte
{
    LittleEndian,
    BigEndian,
}
static class Platform
{
    public static readonly Endianness Endianness =
        BitConverter.IsLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian;
    public static Endianness GetEndianness(bool network) => network ? Endianness.BigEndian : Endianness;
}
