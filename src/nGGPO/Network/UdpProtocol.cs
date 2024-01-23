using System.Net;
using System.Threading.Channels;
using nGGPO.DataStructure;
using nGGPO.Input;
using nGGPO.Network.Messages;
using nGGPO.Utils;

namespace nGGPO.Network;

partial class UdpProtocol : IDisposable
{
    /*
     * Network transmission information
     */
    readonly Udp udp;
    readonly IPEndPoint peerAddress;
    readonly SocketAddress peerSocketAddress;
    readonly ushort magicNumber;
    readonly int queue;
    ushort remoteMagicNumber;
    bool connected;
    int sendLatency;

    Channel<QueueEntry> sendQueue;
    CancellationTokenSource sendQueueCancellation = new();

    /*
     * Stats
     */
    int roundTripTime;
    int packetsSent;

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
    readonly CircularBuffer<GameInput> pendingOutput;
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
    readonly CircularBuffer<UdpEvent> eventQueue;

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
        sendQueue = CircularBuffer.CreateChannel<QueueEntry>();
        pendingOutput = new();
        eventQueue = new();

        magicNumber = Rnd.MagicNumber();

        peerConnectStatus = new ConnectStatus[Max.MsgPlayers];
        for (var i = 0; i < peerConnectStatus.Length; i++)
            peerConnectStatus[i].LastFrame = Frame.NullValue;

        sendLatency = Platform.GetConfigInt("ggpo.network.delay");

        this.timesync = timesync;
        this.random = random;
        this.udp = udp;
        this.queue = queue;
        this.peerAddress = peerAddress;
        this.peerSocketAddress = peerAddress.Serialize();
        this.localConnectStatus = localConnectStatus;

        this.udp.OnMessage += OnMsgEventHandler;
    }

    async ValueTask OnMsgEventHandler(UdpMsg msg, SocketAddress from, CancellationToken ct)
    {
        if (peerSocketAddress.Equals(from))
            await OnMsg(msg);
    }

    public void Dispose()
    {
        sendQueue.Writer.Complete();
        udp.OnMessage -= OnMsgEventHandler;
        Disconnect();
    }

    public ValueTask SendInput(in GameInput input)
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

    InputMsg CreateInputMsg()
    {
        if (pendingOutput.IsEmpty)
            return new();

        var input = InputCompressor.WriteCompressed(
            ref lastAckedInput,
            in pendingOutput,
            ref lastSentInput
        );

        input.AckFrame = lastReceivedInput.Frame;
        input.DisconnectRequested = currentState is not StateEnum.Disconnected;

        if (localConnectStatus.Length > 0)
            localConnectStatus.CopyTo(input.PeerConnectStatus);

        return input;
    }

    ValueTask SendPendingOutput()
    {
        Tracer.Assert(
            Max.InputBytes * Max.MsgPlayers * Mem.ByteSize
            <
            1 << BitVector.BitOffset.NibbleSize
        );

        var input = CreateInputMsg();

        UdpMsg msg = new()
        {
            Header =
            {
                Type = MsgType.Input,
            },
            Input = input,
        };

        return SendMsg(ref msg);
    }

    public void SendInputAck(out UdpMsg msg) =>
        msg = new(MsgType.InputAck)
        {
            InputAck = new()
            {
                AckFrame = lastReceivedInput.Frame,
            },
        };

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

    void SendSyncRequest(out UdpMsg msg)
    {
        state.Sync.Random = random.NextUInt();
        msg = new(MsgType.SyncRequest)
        {
            SyncRequest = new()
            {
                RandomRequest = state.Sync.Random,
            },
        };
    }

    ValueTask SendMsg(ref UdpMsg msg)
    {
        LogMsg("send", msg);

        packetsSent++;
        LastSendTime = Platform.GetCurrentTimeMS();

        msg.Header.Magic = magicNumber;
        msg.Header.SequenceNumber = nextSendSeq++;

        return sendQueue.Writer.WriteAsync(new()
        {
            QueueTime = Platform.GetCurrentTimeMS(),
            DestAddr = peerSocketAddress,
            Msg = msg,
        }, sendQueueCancellation.Token);
    }

    async Task OnMsg(UdpMsg msg)
    {
        var seq = msg.Header.SequenceNumber;
        if (msg.Header.Type is not MsgType.SyncRequest and not MsgType.SyncReply)
        {
            if (msg.Header.Magic != remoteMagicNumber)
            {
                LogMsg("recv rejecting", in msg);
                return;
            }

            var skipped = (ushort)(seq - nextRecvSeq);
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
        var sendReply = false;
        UdpMsg replyMsg = new();

        switch (msg.Header.Type)
        {
            case MsgType.SyncRequest:
                handled = OnSyncRequest(ref msg, ref replyMsg, out sendReply);
                break;
            case MsgType.SyncReply:
                handled = OnSyncReply(msg, ref replyMsg, out sendReply);
                break;
            case MsgType.Input:
                handled = OnInput(msg);
                break;
            case MsgType.QualityReport:
                handled = OnQualityReport(msg, out replyMsg, out sendReply);
                break;
            case MsgType.QualityReply:
                handled = OnQualityReply(msg);
                break;
            case MsgType.InputAck:
                handled = OnInputAck(msg);
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

        if (sendReply)
            await SendMsg(ref replyMsg);

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

    bool OnInput(UdpMsg msg)
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
        if (msg.Input.InputSize > 0)
        {
            // TODO: remove delegate allocation with OnParsedInput
            InputCompressor.DecompressInput(ref msg.Input, ref lastReceivedInput, OnParsedInput);
        }

        Tracer.Assert(lastReceivedInput.Frame >= lastAckedInput.Frame);

        /*
         * Get rid of our buffered input
         */
        OnInputAck(msg);

        return true;
    }

    void OnParsedInput()
    {
        /*
         * Send the event to the emulator
         */
        UdpEvent evt = new(UdpEventType.Input)
        {
            Input = lastReceivedInput,
        };


        state.Running.LastInputPacketRecvTime = (uint)Platform.GetCurrentTimeMS();

        Tracer.Log("Sending frame {0} to emu queue {1} ({2}).\n",
            lastReceivedInput.Frame,
            queue,
            lastAckedInput.Buffer.ToString()
        );

        QueueEvent(evt);
    }

    bool OnInputAck(in UdpMsg msg)
    {
        while (!pendingOutput.IsEmpty && pendingOutput.Peek().Frame < msg.InputAck.AckFrame)
        {
            Tracer.Log("Throwing away pending output frame %d\n", pendingOutput.Peek().Frame);
            lastAckedInput = pendingOutput.Pop();
        }

        return true;
    }

    bool OnQualityReply(in UdpMsg msg)
    {
        roundTripTime = (int)(Platform.GetCurrentTimeMS() - msg.QualityReply.Pong);
        return true;
    }

    bool OnQualityReport(in UdpMsg msg, out UdpMsg newMsg, out bool sendMsg)
    {
        newMsg = new(MsgType.QualityReply)
        {
            QualityReply = new()
            {
                Pong = msg.QualityReport.Ping,
            },
        };

        sendMsg = true;

        remoteFrameAdvantage = msg.QualityReport.FrameAdvantage;

        return true;
    }

    void QueueEvent(UdpEvent evt)
    {
        LogEvent("Queuing event", evt);
        eventQueue.Push(evt);
    }

    bool OnSyncReply(UdpMsg msg, ref UdpMsg replyMsg, out bool sendReply)
    {
        sendReply = false;
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
                    Count = NumSyncPackets - (int)state.Sync.RoundtripsRemaining,
                },
            };

            QueueEvent(evt);
            sendReply = true;
            SendSyncRequest(out replyMsg);
        }

        return true;
    }

    public bool OnSyncRequest(ref UdpMsg msg, ref UdpMsg replyMsg, out bool sendReply)
    {
        if (remoteMagicNumber is not 0 && msg.Header.Magic != remoteMagicNumber)
        {
            Tracer.Log("Ignoring sync request from unknown endpoint ({0} != {1}).\n",
                msg.Header.Magic, remoteMagicNumber);
            sendReply = false;
            return false;
        }

        replyMsg = new UdpMsg(MsgType.SyncReply)
        {
            SyncReply = new()
            {
                RandomReply = msg.SyncRequest.RandomRequest,
            },
        };

        sendReply = true;
        return true;
    }

    async Task PumpSendQueue()
    {
        await foreach (var entry in sendQueue.Reader.ReadAllAsync(sendQueueCancellation.Token))
        {
            if (sendLatency > 0)
            {
                // should really come up with a gaussian distributation based on the configured
                // value, but this will do for now.
                int jitter = (sendLatency * 2 / 3) + ((random.Next() % sendLatency) / 3);
                if (Platform.GetCurrentTimeMS() < entry.QueueTime + jitter)
                    break;
            }

            await udp.SendTo(entry.DestAddr, entry.Msg, sendQueueCancellation.Token);
        }
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
        localFrameAdvantage = (int)remoteFrame - localFrame;
    }


    public void GetNetworkStats(ref NetworkStats stats)
    {
        stats.Ping = roundTripTime;
        stats.SendQueueLen = pendingOutput.Size;
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
        currentState = StateEnum.Syncing;
        throw new NotImplementedException();
    }

    public bool IsInitialized()
    {
        throw new NotImplementedException();
    }
}
