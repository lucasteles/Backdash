using System;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using nGGPO.Network.Messages;
using nGGPO.Types;

namespace nGGPO.Network;

partial class UdpProtocol : IPollLoopSink, IDisposable
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
    RingBuffer<QueueEntry> sendQueue;

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
    readonly ConnectStatus[] localConnectStatus;
    readonly ConnectStatus[] peerConnectStatus;
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
    readonly RingBuffer<GameInput> pendingOutput;
    GameInput lastReceivedInput;
    GameInput lastSentInput;
    GameInput lastAckedInput;


    ushort nextSendSeq;
    ushort nextRecvSeq;

    public long LastSendTime { get; private set; }
    public long LastRecvTime { get; private set; }
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
    readonly RingBuffer<UdpEvent> eventQueue;

    public UdpProtocol(
        TimeSync timesync,
        Udp udp,
        int queue,
        IPEndPoint peerAddress,
        ConnectStatus[] localConnectStatus)
    {
        lastReceivedInput = GameInput.Null;
        lastSentInput = GameInput.Null;
        lastAckedInput = GameInput.Null;
        ooPacket = new();
        sendQueue = new();
        pendingOutput = new();
        eventQueue = new();

        magicNumber = Magic.Number();

        peerConnectStatus = new ConnectStatus[Max.UdpMsgPlayers];
        for (var i = 0; i < peerConnectStatus.Length; i++)
            peerConnectStatus[i].LastFrame = -1;

        sendLatency = Platform.GetConfigInt("ggpo.network.delay");
        oopPercent = Platform.GetConfigInt("ggpo.oop.percent");

        this.timesync = timesync;
        this.udp = udp;
        this.queue = queue;
        this.peerAddress = peerAddress;
        this.localConnectStatus = localConnectStatus;
    }

    public void Dispose() => sendQueue.Clear();

    public Task SendInput(in GameInput input)
    {
        if (currentState is StateEnum.Running)
        {
            /*
             * Check to see if this is a good time to adjust for the rift...
             */
            timesync.AdvanceFrame(in input, localFrameAdvantage, remoteFrameAdvantage);

            /*
             * Save this input packet
             *
             * XXX: This queue may fill up for spectators who do not ack input packets in a timely
             * manner.  When this happens, we can either resize the queue (ug) or disconnect them
             * (better, but still ug).  For the meantime, make this queue really big to decrease
             * the odds of this happening...
             */
            pendingOutput.Push(in input);
        }

        return SendPendingOutput();
    }

    Task SendPendingOutput()
    {
        var offset = 0;
        UdpMsg msg = new()
        {
            Header =
            {
                Type = MsgType.Input,
            },
            Input =
            {
                StartFrame = 0,
                InputSize = 0,
            },
        };

        if (!pendingOutput.IsEmpty)
        {
            ref var front = ref pendingOutput.Peek();
            using var buffer = Mem.CreateBuffer(front.Size);
            msg.Input.StartFrame = (uint) front.Frame;
            msg.Input.InputSize = (byte) front.Size;
            msg.Input.Bits = buffer.Bytes;
            var last = lastAckedInput;

            Trace.Assert(last.IsNull || last.Frame + 1 == msg.Input.StartFrame);
            for (var i = 0; i < pendingOutput.Count; i++)
            {
                ref var current = ref pendingOutput.Value(i);
                if (Mem.BytesEqual(current.Bits, last.Bits))
                    continue;

                Trace.Assert(
                    GameInput.MaxBytes * Max.Players * 8
                    <
                    1 << BitVector.NibbleSize
                );

                for (var j = 0; j < current.Size * 8; j++)
                {
                    Trace.Assert(j < 1 << BitVector.NibbleSize);
                    if (current.Value(j) == last.Value(j))
                        continue;

                    BitVector.SetBit(msg.Input.Bits, ref offset);
                    if (current.Value(j))
                        BitVector.SetBit(msg.Input.Bits, ref offset);
                    else
                        BitVector.ClearBit(msg.Input.Bits, ref offset);

                    BitVector.WriteNibblet(msg.Input.Bits, j, ref offset);
                }

                BitVector.ClearBit(msg.Input.Bits, ref offset);
                last = lastSentInput = current;
            }
        }

        msg.Input.AckFrame = lastReceivedInput.Frame;
        msg.Input.NumBits = (ushort) offset;
        Trace.Assert(offset < Max.CompressedBits);

        msg.Input.DisconnectRequested = currentState is not StateEnum.Disconnected;
        var status = ArrayPool<ConnectStatus>.Shared.Rent(Max.UdpMsgPlayers);

        if (localConnectStatus.Length > 0)
            localConnectStatus.CopyTo(status, 0);

        msg.Input.PeerConnectStatus = status;
        return SendMsg(msg).ContinueWith(_ =>
            ArrayPool<ConnectStatus>.Shared.Return(status)
        );
    }

    Task SendMsg(UdpMsg msg)
    {
        LogMsg("send", msg);

        packetsSent++;
        LastSendTime = Platform.GetCurrentTimeMS();
        bytesSent += msg.PacketSize();

        msg.Header.Magic = magicNumber;
        msg.Header.SequenceNumber = nextSendSeq++;

        sendQueue.Push(new()
        {
            QueueTime = Platform.GetCurrentTimeMS(),
            DestAddr = peerAddress,
            Msg = msg,
        });

        return PumpSendQueue();
    }

    Task PumpSendQueue()
    {
        throw new NotImplementedException();
    }

    void LogMsg(string send, UdpMsg msg)
    {
        throw new NotImplementedException();
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

    public bool HandlesMsg(IPEndPoint from, in UdpMsg msg)
    {
        throw new NotImplementedException();
    }

    public void OnMsg(in UdpMsg msg, int len)
    {
        throw new NotImplementedException();
    }

    public Task<bool> OnLoopPoll(object? value)
    {
        throw new NotImplementedException();
    }
}