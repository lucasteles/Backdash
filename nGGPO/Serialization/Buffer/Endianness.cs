using System;
using System.Buffers.Binary;

namespace nGGPO;

public static class Endianness
{
    public static long HostToNetworkOrder(long host) => BitConverter.IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(host)
        : host;

    public static int HostToNetworkOrder(int host) => BitConverter.IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(host)
        : host;

    public static short HostToNetworkOrder(short host) => BitConverter.IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(host)
        : host;

    public static ulong HostToNetworkOrder(ulong host) => BitConverter.IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(host)
        : host;

    public static uint HostToNetworkOrder(uint host) => BitConverter.IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(host)
        : host;

    public static ushort HostToNetworkOrder(ushort host) => BitConverter.IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(host)
        : host;


    public static long NetworkToHostOrder(long network) => HostToNetworkOrder(network);
    public static int NetworkToHostOrder(int network) => HostToNetworkOrder(network);
    public static short NetworkToHostOrder(short network) => HostToNetworkOrder(network);
    public static ulong NetworkToHostOrder(ulong network) => HostToNetworkOrder(network);
    public static uint NetworkToHostOrder(uint network) => HostToNetworkOrder(network);
    public static ushort NetworkToHostOrder(ushort network) => HostToNetworkOrder(network);
}