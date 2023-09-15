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
        short n => As<short, T>(HostToNetworkOrder(n)),
        int n => As<int, T>(HostToNetworkOrder(n)),
        long n => As<long, T>(HostToNetworkOrder(n)),

        ushort n => As<ushort, T>(HostToNetworkOrder(n)),
        uint n => As<uint, T>(HostToNetworkOrder(n)),
        ulong n => As<ulong, T>(HostToNetworkOrder(n)),

        _ => host,
    };

    public static bool IsReordable(Type host) =>
        host == typeof(short)
        || host == typeof(int)
        || host == typeof(long)
        || host == typeof(ushort)
        || host == typeof(uint)
        || host == typeof(ulong);

    public static long NetworkToHostOrder(long network) => HostToNetworkOrder(network);
    public static int NetworkToHostOrder(int network) => HostToNetworkOrder(network);
    public static short NetworkToHostOrder(short network) => HostToNetworkOrder(network);
    public static ulong NetworkToHostOrder(ulong network) => HostToNetworkOrder(network);
    public static uint NetworkToHostOrder(uint network) => HostToNetworkOrder(network);
    public static ushort NetworkToHostOrder(ushort network) => HostToNetworkOrder(network);

    public static T TryNetworkToHostOrder<T>(T network) where T : unmanaged =>
        TryHostToNetworkOrder(network);
}