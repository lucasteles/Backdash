using System.Net;
using System.Threading.Channels;
using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network.Messages;
using nGGPO.Utils;

namespace nGGPO.Network.Protocol;

sealed partial class UdpProtocol : IDisposable
{
    /*
     * Network transmission information
     */
    readonly UdpPeerClient<UdpMsg> udp;
    readonly ushort magicNumber;
    readonly QueueIndex queue;
    ushort remoteMagicNumber;
    bool connected;
    public int SendLatency { get; set; }
    public IPEndPoint PeerEndPoint { get; }
    public SocketAddress PeerAddress { get; }

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
    ProtocolState currentProtocolState;

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
    public long LastReceivedTime { get; private set; }
    public long ShutdownTimeout { get; set; }
    public int DisconnectTimeout { get; set; }
    public int DisconnectNotifyStart { get; set; }
    public bool DisconnectEventSent { get; set; }
    public bool DisconnectNotifySent { get; set; }

    /*
     * Rift synchronization.
     */
    readonly TimeSync timeSync;
    readonly Random random;

    /*
     * Event queue
     */
    readonly CircularBuffer<UdpEvent> eventQueue;

    public UdpProtocol(
        TimeSync timeSync,
        Random random,
        UdpPeerClient<UdpMsg> udp,
        QueueIndex queue,
        IPEndPoint peerAddress,
        ConnectStatus[] localConnectStatus)
    {
        magicNumber = MagicNumber.Generate();

        lastReceivedInput = GameInput.Empty;
        lastSentInput = GameInput.Empty;
        lastAckedInput = GameInput.Empty;
        sendQueue = CircularBuffer.CreateChannel<QueueEntry>();
        pendingOutput = new();
        eventQueue = new();

        peerConnectStatus = new ConnectStatus[Max.MsgPlayers];
        for (var i = 0; i < peerConnectStatus.Length; i++)
            peerConnectStatus[i].LastFrame = Frame.NullValue;

        this.timeSync = timeSync;
        this.random = random;
        this.queue = queue;
        PeerEndPoint = peerAddress;
        PeerAddress = peerAddress.Serialize();

        this.localConnectStatus = localConnectStatus;

        this.udp = udp;
        this.udp.OnMessage += OnMsgEventHandler;
    }

    async ValueTask OnMsgEventHandler(UdpMsg msg, SocketAddress from, CancellationToken ct)
    {
        if (PeerAddress.Equals(from))
            await OnMsg(msg).ConfigureAwait(false);
    }

    public void Dispose()
    {
        sendQueueCancellation.Cancel();
        sendQueueCancellation.Dispose();
        sendQueue.Writer.Complete();
        udp.OnMessage -= OnMsgEventHandler;
        Disconnect();
    }

    public ValueTask SendInput(in GameInput input)
    {
        if (currentProtocolState is ProtocolState.Running)
        {
            /*
             * Check to see if this is a good time to adjust for the rift...
             */
            timeSync.AdvanceFrame(in input, localFrameAdvantage, remoteFrameAdvantage);

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
        input.DisconnectRequested = currentProtocolState is not ProtocolState.Disconnected;

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

        UdpMsg msg = new(MsgType.Input)
        {
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

    public bool GetEvent(out UdpEvent? e)
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
        currentProtocolState = ProtocolState.Disconnected;
        ShutdownTimeout = TimeStamp.GetMilliseconds() + UdpShutdownTimer;
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
        LastSendTime = TimeStamp.GetMilliseconds();

        msg.Header.Magic = magicNumber;
        msg.Header.SequenceNumber = nextSendSeq++;

        return sendQueue.Writer.WriteAsync(new()
        {
            QueueTime = TimeStamp.GetMilliseconds(),
            DestAddr = PeerAddress,
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
            await SendMsg(ref replyMsg).ConfigureAwait(false);

        if (handled)
        {
            LastReceivedTime = TimeStamp.GetMilliseconds();
            if (DisconnectNotifySent && currentProtocolState is ProtocolState.Running)
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
            if (currentProtocolState is not ProtocolState.Disconnected && !DisconnectEventSent)
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
            // LATER: remove delegate allocation with OnParsedInput
            onParsedInputCache ??= OnParsedInput;
            InputCompressor.DecompressInput(ref msg.Input, ref lastReceivedInput, onParsedInputCache);
        }

        Tracer.Assert(lastReceivedInput.Frame >= lastAckedInput.Frame);

        /*
         * Get rid of our buffered input
         */
        OnInputAck(msg);

        return true;
    }

    Action? onParsedInputCache;

    void OnParsedInput()
    {
        /*
         * Send the event to the emulator
         */
        UdpEvent evt = new(UdpEventType.Input)
        {
            Input = lastReceivedInput,
        };


        state.Running.LastInputPacketRecvTime = (uint)TimeStamp.GetMilliseconds();

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
        roundTripTime = (int)(TimeStamp.GetMilliseconds() - msg.QualityReply.Pong);
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
        if (currentProtocolState is not ProtocolState.Syncing)
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
            QueueEvent(new(UdpEventType.Synchronized));
            currentProtocolState = ProtocolState.Running;
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

    public async Task StartPumping(CancellationToken cancellation)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(sendQueueCancellation.Token, cancellation);

        await foreach (var entry in sendQueue.Reader.ReadAllAsync(cts.Token).ConfigureAwait(false))
        {
            if (SendLatency > 0)
            {
                // should really come up with a gaussian distribution based on the configured
                // value, but this will do for now.
                int jitter = (SendLatency * 2 / 3) + (random.Next() % SendLatency / 3);
                if (TimeStamp.GetMilliseconds() < entry.QueueTime + jitter)
                    break;
            }

            await udp.SendTo(entry.DestAddr, entry.Msg, sendQueueCancellation.Token).ConfigureAwait(false);
        }
    }

    // require idle input should be a configuration parameter
    public int RecommendFrameDelay() =>
        timeSync.RecommendFrameWaitDuration(false);

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
        currentProtocolState = ProtocolState.Syncing;
        throw new NotImplementedException();
    }

    public bool IsInitialized()
    {
        throw new NotImplementedException();
    }
}
