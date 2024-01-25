using System.Net;
using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network.Client;
using nGGPO.Network.Messages;
using nGGPO.Network.Protocol.Gear;
using nGGPO.Utils;

namespace nGGPO.Network.Protocol;

using static ProtocolConstants;

sealed class UdpProtocol : IPeerClientObserver<ProtocolMessage>, IDisposable
{
    /*
     * Network transmission information
     */
    readonly UdpPeerClient<ProtocolMessage> udp;
    readonly QueueIndex queue;
    ushort remoteMagicNumber;
    bool connected;
    public IPEndPoint PeerEndPoint { get; }
    public SocketAddress PeerAddress { get; }

    /*
     * Stats
     */
    int roundTripTime;

    /*
     * The state machine
     */
    readonly ConnectStatus[] localConnectStatus;
    readonly ConnectStatus[] peerConnectStatus;
    readonly ProtocolState.Udp state = new();
    ProtocolState.Name currentProtocolState;

    /*
     * Fairness.
     */
    int localFrameAdvantage;
    int remoteFrameAdvantage;


    /*
     * Packet loss...
     */
    GameInput lastReceivedInput;
    GameInput lastAckedInput;
    ushort nextRecvSeq;

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

    /*
     * Event queue
     */
    readonly CircularBuffer<ProtocolEventData> eventQueue;

    // services
    readonly Random random;
    readonly InputCompressor inputCompressor;
    readonly InputProcessor inputProcessor;
    readonly MessageOutbox outbox;

    public UdpProtocol(TimeSync timeSync,
        Random random,
        UdpPeerClient<ProtocolMessage> udp,
        QueueIndex queue,
        IPEndPoint peerAddress,
        ConnectStatus[] localConnectStatus,
        InputCompressor inputCompressor,
        int networkDelay = 0
    )
    {
        lastReceivedInput = GameInput.Empty;
        lastAckedInput = GameInput.Empty;
        eventQueue = new();

        peerConnectStatus = new ConnectStatus[Max.MsgPlayers];
        for (var i = 0; i < peerConnectStatus.Length; i++)
            peerConnectStatus[i].LastFrame = Frame.NullValue;

        this.localConnectStatus = localConnectStatus;
        this.inputCompressor = inputCompressor;
        this.timeSync = timeSync;
        this.random = random;
        this.queue = queue;

        PeerEndPoint = peerAddress;
        PeerAddress = peerAddress.Serialize();

        this.udp = udp;
        outbox = new(PeerAddress, this.udp, this.random)
        {
            SendLatency = networkDelay,
        };
        inputProcessor = new(this.timeSync, inputCompressor, this.localConnectStatus, outbox);
    }

    public ValueTask OnMessage(
        UdpPeerClient<ProtocolMessage> sender,
        ProtocolMessage message,
        SocketAddress from,
        CancellationToken stoppingToken
    ) => PeerAddress.Equals(from)
        ? OnMsg(message, stoppingToken)
        : ValueTask.CompletedTask;

    public void Dispose()
    {
        Disconnect();
        outbox.Dispose();
    }

    public ValueTask SendInput(in GameInput input, CancellationToken ct) =>
        inputProcessor.SendInput(in input, currentProtocolState,
            lastReceivedInput, lastAckedInput,
            localFrameAdvantage, remoteFrameAdvantage, ct);

    public void SendInputAck(out ProtocolMessage msg) =>
        msg = new(MsgType.InputAck)
        {
            InputAck = new()
            {
                AckFrame = lastReceivedInput.Frame,
            },
        };

    public bool GetEvent(out ProtocolEventData? e)
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
        currentProtocolState = ProtocolState.Name.Disconnected;
        ShutdownTimeout = TimeStamp.GetMilliseconds() + UdpShutdownTimer;
    }

    void SendSyncRequest(out ProtocolMessage msg)
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

    async ValueTask OnMsg(ProtocolMessage msg, CancellationToken ct)
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
        ProtocolMessage replyMsg = new();

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
            await outbox.SendMsg(ref replyMsg, ct).ConfigureAwait(false);

        if (handled)
        {
            LastReceivedTime = TimeStamp.GetMilliseconds();
            if (DisconnectNotifySent && currentProtocolState is ProtocolState.Name.Running)
            {
                QueueEvent(new(ProtocolEvent.NetworkResumed));
                DisconnectNotifySent = false;
            }
        }
    }

    bool OnInput(ProtocolMessage msg)
    {
        /*
         * If a disconnect is requested, go ahead and disconnect now.
         */
        var disconnectRequested = msg.Input.DisconnectRequested;

        if (disconnectRequested)
        {
            if (currentProtocolState is not ProtocolState.Name.Disconnected && !DisconnectEventSent)
            {
                Tracer.Log("Disconnecting endpoint on remote request.\n");
                QueueEvent(new(ProtocolEvent.Disconnected));
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
            inputCompressor.Decompress(ref msg.Input, ref lastReceivedInput, onParsedInputCache);
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
        ProtocolEventData evt = new(ProtocolEvent.Input)
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

    bool OnInputAck(in ProtocolMessage msg)
    {
        var pendingOutput = inputProcessor.Pending;
        while (!pendingOutput.IsEmpty && pendingOutput.Peek().Frame < msg.InputAck.AckFrame)
        {
            Tracer.Log("Throwing away pending output frame %d\n", pendingOutput.Peek().Frame);
            lastAckedInput = pendingOutput.Pop();
        }

        return true;
    }

    bool OnQualityReply(in ProtocolMessage msg)
    {
        roundTripTime = (int)(TimeStamp.GetMilliseconds() - msg.QualityReply.Pong);
        return true;
    }

    bool OnQualityReport(in ProtocolMessage msg, out ProtocolMessage newMsg, out bool sendMsg)
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

    void QueueEvent(ProtocolEventData evt)
    {
        LogEvent("Queuing event", evt);
        eventQueue.Push(evt);
    }

    bool OnSyncReply(ProtocolMessage msg, ref ProtocolMessage replyMsg, out bool sendReply)
    {
        sendReply = false;
        if (currentProtocolState is not ProtocolState.Name.Syncing)
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
            QueueEvent(new(ProtocolEvent.Connected));
            connected = true;
        }

        Tracer.Log("Checking sync state ({0} round trips remaining).\n",
            state.Sync.RemainingRoundtrips);

        if (--state.Sync.RemainingRoundtrips == 0)
        {
            Tracer.Log("Synchronized!\n");
            QueueEvent(new(ProtocolEvent.Synchronized));
            currentProtocolState = ProtocolState.Name.Running;
            lastReceivedInput.ResetFrame();
            remoteMagicNumber = msg.Header.Magic;
        }
        else
        {
            ProtocolEventData evt = new(ProtocolEvent.Synchronizing)
            {
                Synchronizing = new()
                {
                    Total = NumSyncPackets,
                    Count = NumSyncPackets - (int)state.Sync.RemainingRoundtrips,
                },
            };

            QueueEvent(evt);
            sendReply = true;
            SendSyncRequest(out replyMsg);
        }

        return true;
    }

    public bool OnSyncRequest(ref ProtocolMessage msg, ref ProtocolMessage replyMsg, out bool sendReply)
    {
        if (remoteMagicNumber is not 0 && msg.Header.Magic != remoteMagicNumber)
        {
            Tracer.Log("Ignoring sync request from unknown endpoint ({0} != {1}).\n",
                msg.Header.Magic, remoteMagicNumber);
            sendReply = false;
            return false;
        }

        replyMsg = new ProtocolMessage(MsgType.SyncReply)
        {
            SyncReply = new()
            {
                RandomReply = msg.SyncRequest.RandomRequest,
            },
        };

        sendReply = true;
        return true;
    }

    public async Task StartPumping(CancellationToken ct)
    {
        await outbox.StartPumping(ct);
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
        stats.SendQueueLen = inputProcessor.Pending.Size;
        stats.RemoteFramesBehind = remoteFrameAdvantage;
        stats.LocalFramesBehind = localFrameAdvantage;
    }

    void LogMsg(string send, in ProtocolMessage msg)
    {
        throw new NotImplementedException();
    }

    void LogEvent(string queuingEvent, ProtocolEventData evt)
    {
        throw new NotImplementedException();
    }

    public void Synchronize()
    {
        currentProtocolState = ProtocolState.Name.Syncing;
        throw new NotImplementedException();
    }

    public bool IsInitialized()
    {
        throw new NotImplementedException();
    }
}
