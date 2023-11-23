using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace nGGPO.Network;

public static class Endianness
{
    public static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;

    public static long HostToNetworkOrder(long host) => IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(host)
        : host;

    public static char HostToNetworkOrder(char host) => IsLittleEndian
        ? (char) BinaryPrimitives.ReverseEndianness(host)
        : host;

    public static Int128 HostToNetworkOrder(Int128 host) => IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(host)
        : host;

    public static UInt128 HostToNetworkOrder(UInt128 host) => IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(host)
        : host;

    public static int HostToNetworkOrder(int host) => IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(host)
        : host;

    public static short HostToNetworkOrder(short host) => IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(host)
        : host;

    public static ulong HostToNetworkOrder(ulong host) => IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(host)
        : host;

    public static uint HostToNetworkOrder(uint host) => IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(host)
        : host;

    public static ushort HostToNetworkOrder(ushort host) => IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(host)
        : host;

    static TTo As<TFrom, TTo>(TFrom value) where TFrom : unmanaged where TTo : unmanaged
    {
        var valueRef = value;
        return Unsafe.As<TFrom, TTo>(ref valueRef);
    }

    public static T TryHostToNetworkOrder<T>(T host) where T : unmanaged => host switch
    {
        char n => As<char, T>(HostToNetworkOrder(n)),
        short n => As<short, T>(HostToNetworkOrder(n)),
        int n => As<int, T>(HostToNetworkOrder(n)),
        long n => As<long, T>(HostToNetworkOrder(n)),
        Int128 n => As<Int128, T>(HostToNetworkOrder(n)),
        ushort n => As<ushort, T>(HostToNetworkOrder(n)),
        uint n => As<uint, T>(HostToNetworkOrder(n)),
        ulong n => As<ulong, T>(HostToNetworkOrder(n)),
        UInt128 n => As<UInt128, T>(HostToNetworkOrder(n)),
        _ => host,
    };

    public static long NetworkToHostOrder(long network) => HostToNetworkOrder(network);
    public static int NetworkToHostOrder(int network) => HostToNetworkOrder(network);
    public static short NetworkToHostOrder(short network) => HostToNetworkOrder(network);
    public static ulong NetworkToHostOrder(ulong network) => HostToNetworkOrder(network);
    public static uint NetworkToHostOrder(uint network) => HostToNetworkOrder(network);
    public static ushort NetworkToHostOrder(ushort network) => HostToNetworkOrder(network);
    public static char NetworkToHostOrder(char network) => HostToNetworkOrder(network);
    public static Int128 NetworkToHostOrder(Int128 network) => HostToNetworkOrder(network);
    public static UInt128 NetworkToHostOrder(UInt128 network) => HostToNetworkOrder(network);

    public static T TryNetworkToHostOrder<T>(T network) where T : unmanaged =>
        TryHostToNetworkOrder(network);
}