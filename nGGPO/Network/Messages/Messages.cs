using System.Runtime.InteropServices;
using nGGPO.Types;

namespace nGGPO.Network.Messages;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ConnectStatus
{
    [MarshalAs(UnmanagedType.I1)]
    public bool Disconnected;

    public int LastFrame;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Header
{
    public MsgType Type;
    public ushort Magic;
    public ushort SequenceNumber;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SyncRequest
{
    public uint RandomRequest; /* please reply back with this random data */
    public ushort RemoteMagic;
    public byte RemoteEndpoint;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SyncReply
{
    public uint RandomReply; /* please reply back with this random data */
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct QualityReport
{
    public byte FrameAdvantage; /* what's the other guy's frame advantage? */
    public uint Ping;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct QualityReply
{
    public uint Pong;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct InputMsg
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Max.UdpMsgPlayers)]
    public ConnectStatus[] PeerConnectStatus;

    public int StartFrame;

    [MarshalAs(UnmanagedType.I1)]
    public bool DisconnectRequested;

    public int AckFrame;

    public ushort NumBits;
    public byte InputSize; // XXX: shouldn't be in every single packet!

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Max.CompressedBits)]
    public byte[] Bits; /* must be last */
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct InputAck
{
    public int AckFrame;
}