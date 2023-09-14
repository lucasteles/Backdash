using System;
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
        lastReceivedInput = GameInput.Empty;
        lastSentInput = GameInput.Empty;
        lastAckedInput = GameInput.Empty;
        ooPacket = new();
        sendQueue = new();
        pendingOutput = new();
        eventQueue = new();

        magicNumber = Magic.Number();

        peerConnectStatus = new ConnectStatus[Max.UdpMsgPlayers];
        for (var i = 0; i < peerConnectStatus.Length; i++)
            peerConnectStatus[i].LastFrame = Frame.NullValue;

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
        Trace.Assert(
            GameInput.MaxBytes * Max.Players * Mem.ByteSize
            <
            1 << BitVector.BitOffsetWriter.NibbleSize
        );

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
            var buffer = Mem.Rent(front.Size);
            msg.Input.InputSize = (byte) front.Size;
            msg.Input.StartFrame = front.Frame;
            msg.Input.Bits = buffer;

            var offset = WriteCompressedInput(ref msg.Input.Bits, msg.Input.StartFrame);
            msg.Input.NumBits = (ushort) offset;
            Trace.Assert(offset < Max.CompressedBits);
        }

        msg.Input.AckFrame = lastReceivedInput.Frame;
        msg.Input.DisconnectRequested = currentState is not StateEnum.Disconnected;

        var status = Mem.Rent<ConnectStatus>(Max.UdpMsgPlayers);
        if (localConnectStatus.Length > 0) localConnectStatus.CopyTo(status, 0);
        msg.Input.PeerConnectStatus = status;

        return SendMsg(msg);
    }

    int WriteCompressedInput(ref byte[] bits, int startFrame)
    {
        BitVector.BitOffsetWriter bitWriter = new(bits);
        var last = lastAckedInput;
        Trace.Assert(last.Frame.IsNull || last.Frame.Next == startFrame);

        for (var i = 0; i < pendingOutput.Count; i++)
        {
            ref var current = ref pendingOutput.Get(i);
            if (current.Bits != last.Bits)
            {
                for (var j = 0; j < current.Bits.BitCount; j++)
                {
                    if (current.Bits[j] == last.Bits[j])
                        continue;

                    bitWriter.SetNext();

                    if (current.Bits[j])
                        bitWriter.SetNext();
                    else
                        bitWriter.ClearNext();

                    bitWriter.WriteNibble(j);
                }
            }

            bitWriter.ClearNext();
            last = lastSentInput = current;
        }

        return bitWriter.Offset;
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
        while (!sendQueue.IsEmpty)
        {
            ref var entry = ref sendQueue.Peek();

            // TODO: the rest

            sendQueue.Pop();
            if (entry.Msg.Header.Type is MsgType.Input)
            {
                Mem.Return(entry.Msg.Input.Bits);
                Mem.Return(entry.Msg.Input.PeerConnectStatus);
            }
        }

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