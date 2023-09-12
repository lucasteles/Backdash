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
    uint RandomRequest; /* please reply back with this random data */
    ushort RemoteMagic;
    byte RemoteEndpoint;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SyncReply
{
    uint RandomReply; /* please reply back with this random data */
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct QualityReport
{
    byte FrameAdvantage; /* what's the other guy's frame advantage? */
    uint Ping;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct QualityReply
{
    uint Pong;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct InputAck
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Max.UdpMsgPlayers)]
    ConnectStatus[] PeerConnectStatus;

    uint StartFrame;

    [MarshalAs(UnmanagedType.I1)]
    bool DisconnectRequested;

    int AckFrame;

    ushort NumBits;
    byte InputSize; // XXX: shouldn't be in every single packet!

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Max.CompressedBits)]
    byte[] Bits; /* must be last */
}