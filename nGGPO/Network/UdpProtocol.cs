using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using nGGPO.Network.Messages;
using nGGPO.Types;

namespace nGGPO.Network;

partial class UdpProtocol : IPollLoopSink
{
    /*
     * Network transmission information
     */
    readonly Udp? udp;
    readonly IPEndPoint peerAddress;
    readonly ushort magicNumber;
    readonly int queue;
    ushort remoteMagicNumber;
    bool connected;
    int sendLatency;
    int oopPercent;
    Packet ooPacket;

    /*
     * Stats
     */
    int roundTripTime;
    int packetsSent;
    int bytesSent;
    int kbpsSent;
    int statsStartTime;

    /*
     * The state machine
     */
    readonly ConnectStatus localConnectStatus;
    readonly StaticBuffer<ConnectStatus> peerConnectStatus = new(Max.UdpMsgPlayers);
    StateEnum currentState;
    object? state;

    /*
     * Fairness.
     */
    int localFrameAdvantage;
    int remoteFrameAdvantage;


    /*
     * Packet loss...
     */
    readonly RingBuffer<GameInput> pendingOutput = new();
    GameInput? lastReceivedInput = null;
    GameInput? lastSentInput = null;
    GameInput? lastAckedInput = null;


    ushort nextSendSeq;
    ushort nextRecvSeq;

    public int LastSendTime { get; set; }
    public int LastRecvTime { get; set; }
    public int ShutdownTimeout { get; set; }
    public int DisconnectTimeout { get; set; }
    public int DisconnectNotifyStart { get; set; }
    public int DisconnectEventSent { get; set; }
    public bool DisconnectNotifySent { get; set; }

    /*
     * Rift synchronization.
     */
    readonly TimeSync timesync;

    /*
     * Event queue
     */
    readonly RingBuffer<UdpEvent> eventQueue = new();

    public UdpProtocol(
        TimeSync timesync,
        Udp udp,
        int queue,
        IPEndPoint peerAddress,
        IReadOnlyList<ConnectStatus> connectStatus
    )
    {
        this.timesync = timesync;
        this.udp = udp;
        this.queue = queue;
        this.peerAddress = peerAddress;
        localConnectStatus = connectStatus[0];
    }


    public void Synchronize()
    {
        if (udp is null) return;
        currentState = StateEnum.Syncing;
        throw new NotImplementedException();
    }

    public bool IsInitialized()
    {
        throw new NotImplementedException();
    }

    public void SendInput(GameInput input)
    {
        throw new NotImplementedException();
    }

    public bool HandlesMsg(IPEndPoint from, UdpMsg msg)
    {
        throw new NotImplementedException();
    }

    public void OnMsg(UdpMsg msg, int len)
    {
        throw new NotImplementedException();
    }

    public Task<bool> OnLoopPoll(object? value)
    {
        throw new NotImplementedException();
    }
}