﻿using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using nGGPO.DataStructure;
using nGGPO.Input;
using nGGPO.Network.Messages;
using nGGPO.Utils;

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
    readonly UdpProtocolState state = new();
    StateEnum currentState;

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
    public long ShutdownTimeout { get; set; }
    public int DisconnectTimeout { get; set; }
    public int DisconnectNotifyStart { get; set; }
    public bool DisconnectEventSent { get; set; }
    public bool DisconnectNotifySent { get; set; }


    /*
     * Rift synchronization.
     */
    readonly TimeSync timesync;
    readonly Random random;

    /*
     * Event queue
     */
    readonly RingBuffer<UdpEvent> eventQueue;

    public UdpProtocol(
        TimeSync timesync,
        Random random,
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

        magicNumber = Rnd.MagicNumber();

        peerConnectStatus = new ConnectStatus[Max.UdpMsgPlayers];
        for (var i = 0; i < peerConnectStatus.Length; i++)
            peerConnectStatus[i].LastFrame = Frame.NullValue;

        sendLatency = Platform.GetConfigInt("ggpo.network.delay");
        oopPercent = Platform.GetConfigInt("ggpo.oop.percent");

        this.timesync = timesync;
        this.random = random;
        this.udp = udp;
        this.queue = queue;
        this.peerAddress = peerAddress;
        this.localConnectStatus = localConnectStatus;
    }

    public void Dispose()
    {
        Disconnect();
        sendQueue.Clear();
    }

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
        Tracer.Assert(
            GameInput.MaxBytes * Max.Players * Mem.ByteSize
            <
            1 << BitVector.BitOffset.NibbleSize
        );

        var input = GetInputMsg();

        input.AckFrame = lastReceivedInput.Frame;
        input.DisconnectRequested = currentState is not StateEnum.Disconnected;
        if (localConnectStatus.Length > 0)
            localConnectStatus.CopyTo(input.PeerConnectStatus);

        UdpMsg msg = new()
        {
            Header =
            {
                Type = MsgType.Input,
            },
            Input = input,
        };

        return SendMsg(msg);

        int WriteCompressedInput(Memory<byte> bits, int startFrame)
        {
            BitVector.BitOffset bitWriter = new(bits);
            var last = lastAckedInput;
            Tracer.Assert(last.Frame.IsNull || last.Frame.Next == startFrame);

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

        InputMsg GetInputMsg()
        {
            if (!pendingOutput.IsEmpty)
            {
                ref var front = ref pendingOutput.Peek();

                InputMsg input = new(front.Size, Max.UdpMsgPlayers)
                {
                    InputSize = (byte) front.Size,
                    StartFrame = front.Frame,
                };

                var offset = WriteCompressedInput(input.Bits.Memory, input.StartFrame);
                input.NumBits = (ushort) offset;
                Tracer.Assert(offset < Max.CompressedBits);

                return input;
            }

            return new(peerCount: Max.UdpMsgPlayers);
        }
    }

    public Task SendInputAck() => SendMsg(
        new(MsgType.InputAck)
        {
            InputAck = new()
            {
                AckFrame = lastReceivedInput.Frame,
            },
        });

    bool GetEvent(out UdpEvent? e)
    {
        if (eventQueue.IsEmpty)
        {
            e = default;
            return false;
        }

        e = eventQueue.Pop();
        return true;
    }

    public void Disconnect()
    {
        currentState = StateEnum.Disconnected;
        ShutdownTimeout = Platform.GetCurrentTimeMS() + UdpShutdownTimer;
    }

    async Task SendSyncRequest()
    {
        state.Sync.Random = random.NextUInt();

        await SendMsg(
            new(MsgType.SyncRequest)
            {
                SyncRequest = new()
                {
                    RandomRequest = state.Sync.Random,
                },
            });
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

    public bool HandlesMsg(IPEndPoint from, in UdpMsg _) =>
        peerAddress.Address.Equals(from.Address)
        && peerAddress.Port == from.Port;

    public async Task OnMsg(UdpMsg msg, int len)
    {
        var seq = msg.Header.SequenceNumber;
        if (msg.Header.Type is not MsgType.SyncRequest and not MsgType.SyncReply)
        {
            if (msg.Header.Magic != remoteMagicNumber)
            {
                LogMsg("recv rejecting", in msg);
                return;
            }

            var skipped = (ushort) (seq - nextRecvSeq);
            if (skipped > MaxSeqDistance)
            {
                Tracer.Log("dropping out of order packet (seq: %d, last seq:%d)\n",
                    seq, nextRecvSeq);
                return;
            }
        }

        nextRecvSeq = seq;
        LogMsg("recv", msg);
        var handled = false;
        switch (msg.Header.Type)
        {
            case MsgType.SyncRequest:
                handled = await OnSyncRequest(msg, len);
                break;
            case MsgType.SyncReply:
                handled = await OnSyncReply(msg, len);
                break;
            case MsgType.Input:
                handled = await OnInput(msg, len);
                break;
            case MsgType.QualityReport:
                handled = await OnQualityReport(msg, len);
                break;
            case MsgType.QualityReply:
                handled = OnQualityReply(msg, len);
                break;
            case MsgType.InputAck:
                handled = OnInputAck(msg, len);
                break;
            case MsgType.KeepAlive:
                handled = true;
                break;
            case MsgType.Invalid:
                Tracer.Fail("Invalid msg in UdpProtocol");
                break;
            default:
                Tracer.Fail("Unknown UdpMsg type.");
                break;
        }

        if (handled)
        {
            LastRecvTime = Platform.GetCurrentTimeMS();
            if (DisconnectNotifySent && currentState is StateEnum.Running)
            {
                QueueEvent(new(UdpEventType.NetworkResumed));
                DisconnectNotifySent = false;
            }
        }
    }

    async Task<bool> OnInput(UdpMsg msg, int len)
    {
        /*
         * If a disconnect is requested, go ahead and disconnect now.
         */
        var disconnectRequested = msg.Input.DisconnectRequested;

        if (disconnectRequested)
        {
            if (currentState is not StateEnum.Disconnected && !DisconnectEventSent)
            {
                Tracer.Log("Disconnecting endpoint on remote request.\n");
                QueueEvent(new(UdpEventType.Disconnected));
                DisconnectEventSent = true;
            }
        }
        else
        {
            /*
             * Update the peer connection status if this peer is still considered to be part
             * of the network.
             */
            var remoteStatus = msg.Input.PeerConnectStatus;
            for (var i = 0; i < peerConnectStatus.Length; i++)
            {
                Tracer.Assert(remoteStatus[i].LastFrame >= peerConnectStatus[i].LastFrame);
                peerConnectStatus[i].Disconnected =
                    peerConnectStatus[i].Disconnected
                    || remoteStatus[i].Disconnected;

                peerConnectStatus[i].LastFrame = Math.Max(
                    peerConnectStatus[i].LastFrame,
                    remoteStatus[i].LastFrame
                );
            }
        }

        // /*
        //  * Decompress the input.
        //  */
        var lastReceivedFrameNumber = lastAckedInput.Frame;

        if (msg.Input.Bits.Length > 0)
        {
            var numBits = msg.Input.NumBits;
            var currentFrame = msg.Input.StartFrame;

            lastReceivedInput.Size = msg.Input.InputSize;

            if (lastReceivedInput.Frame < 0)
                lastReceivedInput.SetFrame(new(msg.Input.StartFrame - 1));

            BitVector.BitOffset bitVector = new(msg.Input.Bits.Memory);

            while (bitVector.Offset < numBits)
            {
                /*
                 * Keep walking through the frames (parsing bits) until we reach
                 * the inputs for the frame right after the one we're on.
                 */
                Trace.Assert(currentFrame <= lastReceivedInput.Frame.Next);
                var useInputs = currentFrame == lastReceivedInput.Frame.Next;

                while (bitVector.Read())
                {
                    var on = bitVector.Read();
                    var button = bitVector.ReadNibble();
                    if (!useInputs) continue;
                    if (on)
                        lastReceivedInput.Bits.Set(button);
                    else
                        lastReceivedInput.Bits.Clear(button);
                }

                Tracer.Assert(bitVector.Offset <= numBits);

                /*
                 * Now if we want to use these inputs, go ahead and send them to
                 * the emulator.
                 */

                if (useInputs)
                {
                    /*
                     * Move forward 1 frame in the stream.
                     */
                    Tracer.Assert(currentFrame == lastReceivedInput.Frame.Next);
                    lastReceivedInput.SetFrame(new(currentFrame));

                    /*
                     * Send the event to the emualtor
                     */
                    UdpEvent evt = new(UdpEventType.Input)
                    {
                        Input = lastAckedInput,
                    };

                    state.Running.LastInputPacketRecvTime = (uint) Platform.GetCurrentTimeMS();

                    Tracer.Log("Sending frame {0} to emu queue {1} ({2}).\n",
                        lastReceivedInput.Frame, queue, lastAckedInput.Bits.ToString());

                    QueueEvent(evt);
                }
                else
                {
                    Tracer.Log("Skipping past frame:(%d) current is %d.\n",
                        currentFrame, lastReceivedInput.Frame);
                }

                /*
                 * Move forward 1 frame in the input stream.
                 */
                currentFrame++;
            }
        }

        Tracer.Assert(lastReceivedInput.Frame >= lastReceivedFrameNumber);

        /*
         * Get rid of our buffered input
         */

        while (!pendingOutput.IsEmpty && pendingOutput.Peek().Frame < msg.Input.AckFrame)
        {
            Tracer.Log("Throwing away pending output frame %d\n", pendingOutput.Peek().Frame);
            lastAckedInput = pendingOutput.Pop();
        }

        return true;
    }

    bool OnInputAck(UdpMsg msg, int len)
    {
        while (!pendingOutput.IsEmpty && pendingOutput.Peek().Frame < msg.InputAck.AckFrame)
        {
            Tracer.Log("Throwing away pending output frame %d\n", pendingOutput.Peek().Frame);
            lastAckedInput = pendingOutput.Pop();
        }

        return true;
    }

    bool OnQualityReply(UdpMsg msg, int len)
    {
        roundTripTime = (int) (Platform.GetCurrentTimeMS() - msg.QualityReply.Pong);
        return true;
    }

    async Task<bool> OnQualityReport(UdpMsg msg, int len)
    {
        UdpMsg reply = new(MsgType.QualityReply)
        {
            QualityReply = new()
            {
                Pong = msg.QualityReport.Ping,
            },
        };

        await SendMsg(reply);

        remoteFrameAdvantage = msg.QualityReport.FrameAdvantage;
        return true;
    }

    void QueueEvent(UdpEvent evt)
    {
        LogEvent("Queuing event", evt);
        eventQueue.Push(evt);
    }

    async Task<bool> OnSyncReply(UdpMsg msg, int len)
    {
        if (currentState is not StateEnum.Syncing)
        {
            Tracer.Log("Ignoring SyncReply while not synching.\n");
            return msg.Header.Magic == remoteMagicNumber;
        }

        if (msg.SyncReply.RandomReply != state.Sync.Random)
        {
            Tracer.Log("sync reply {0} != {1}.  Keep looking...\n",
                msg.SyncReply.RandomReply, state.Sync.Random);
            return false;
        }

        if (!connected)
        {
            QueueEvent(new(UdpEventType.Connected));
            connected = true;
        }

        Tracer.Log("Checking sync state ({0} round trips remaining).\n",
            state.Sync.RoundtripsRemaining);

        if (--state.Sync.RoundtripsRemaining == 0)
        {
            Tracer.Log("Synchronized!\n");
            QueueEvent(new(UdpEventType.Synchronzied));
            currentState = StateEnum.Running;
            lastReceivedInput.ResetFrame();
            remoteMagicNumber = msg.Header.Magic;
        }
        else
        {
            UdpEvent evt = new(UdpEventType.Synchronizing)
            {
                Synchronizing = new()
                {
                    Total = NumSyncPackets,
                    Count = NumSyncPackets - (int) state.Sync.RoundtripsRemaining,
                },
            };
            QueueEvent(evt);
            await SendSyncRequest();
        }

        return true;
    }

    public async Task<bool> OnSyncRequest(UdpMsg msg, int len)
    {
        if (remoteMagicNumber is not 0 && msg.Header.Magic != remoteMagicNumber)
        {
            Tracer.Log("Ignoring sync request from unknown endpoint ({0} != {1}).\n",
                msg.Header.Magic, remoteMagicNumber);
            return false;
        }

        await SendMsg(new(MsgType.SyncReply)
        {
            SyncReply = new()
            {
                RandomReply = msg.SyncRequest.RandomRequest,
            },
        });


        return true;
    }

    Task PumpSendQueue()
    {
        while (!sendQueue.IsEmpty)
        {
            ref var entry = ref sendQueue.Peek();

            // TODO: everything else

            sendQueue.Pop();
            entry.Msg.Dispose();
        }

        throw new NotImplementedException();
    }


    public int RecommendFrameDelay()
    {
        // XXX: require idle input should be a configuration parameter
        return timesync.RecommendFrameWaitDuration(false);
    }

    void SetLocalFrameNumber(int localFrame)
    {
        /*
         * Estimate which frame the other guy is one by looking at the
         * last frame they gave us plus some delta for the one-way packet
         * trip time.
         */
        var remoteFrame = lastReceivedInput.Frame + roundTripTime * 60 / 1000;

        /*
         * Our frame advantage is how many frames *behind* the other guy
         * we are.  Counter-intuitive, I know.  It's an advantage because
         * it means they'll have to predict more often and our moves will
         * pop more frequently.
         */
        localFrameAdvantage = (int) remoteFrame - localFrame;
    }


    public void GetNetworkStats(ref NetworkStats stats)
    {
        stats.Ping = roundTripTime;
        stats.SendQueueLen = pendingOutput.Count;
        stats.KbpsSent = kbpsSent;
        stats.RemoteFramesBehind = remoteFrameAdvantage;
        stats.LocalFramesBehind = localFrameAdvantage;
    }

    void LogMsg(string send, in UdpMsg msg)
    {
        throw new NotImplementedException();
    }

    void LogEvent(string queuingEvent, UdpEvent evt)
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

    public Task<bool> OnLoopPoll(object? value)
    {
        throw new NotImplementedException();
    }
}