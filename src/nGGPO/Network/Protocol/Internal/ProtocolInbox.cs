using System.Net;
using nGGPO.Input;
using nGGPO.Network.Client;
using nGGPO.Network.Messages;
using nGGPO.Utils;

namespace nGGPO.Network.Protocol.Internal;

using static ProtocolConstants;

sealed class ProtocolInbox(
    ProtocolState state,
    InputCompressor compressor,
    ProtocolInputProcessor inputProcessor,
    ProtocolEventDispatcher events,
    IMessageSender messageSender,
    Random random,
    ProtocolLogger logger
) : IUdpObserver<ProtocolMessage>
{
    ushort remoteMagicNumber;
    ushort nextRecvSeq;

    Action? onParsedInputCache;

    GameInput lastReceivedInput = GameInput.Empty;
    public long LastReceivedTime { get; private set; }

    GameInput lastAckedInput = GameInput.Empty;

    public GameInput LastReceivedInput => lastReceivedInput;
    public GameInput LastAckedInput => lastAckedInput;

    public async ValueTask OnUdpMessage(
        UdpClient<ProtocolMessage> _,
        ProtocolMessage message,
        SocketAddress from,
        CancellationToken stoppingToken)
    {
        if (!from.Equals(state.PeerAddress.Address))
            return;

        var seqNum = message.Header.SequenceNumber;
        if (message.Header.Type is not MsgType.SyncRequest and not MsgType.SyncReply)
        {
            if (message.Header.Magic != remoteMagicNumber)
            {
                logger.LogMsg("recv rejecting", in message);
                return;
            }

            var skipped = (ushort)(seqNum - nextRecvSeq);
            if (skipped > MaxSeqDistance)
            {
                Tracer.Log("dropping out of order packet (seq: %d, last seq:%d)\n",
                    seqNum, nextRecvSeq);
                return;
            }
        }

        nextRecvSeq = seqNum;
        logger.LogMsg("recv", message);
        var handled = false;
        var sendReply = false;
        ProtocolMessage replyMsg = new();

        switch (message.Header.Type)
        {
            case MsgType.SyncRequest:
                handled = OnSyncRequest(ref message, ref replyMsg, out sendReply);
                break;
            case MsgType.SyncReply:
                handled = OnSyncReply(message, ref replyMsg, out sendReply);
                break;
            case MsgType.Input:
                handled = OnInput(message);
                break;
            case MsgType.QualityReport:
                handled = OnQualityReport(message, out replyMsg, out sendReply);
                break;
            case MsgType.QualityReply:
                handled = OnQualityReply(message);
                break;
            case MsgType.InputAck:
                handled = OnInputAck(message);
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
            await messageSender.SendMessage(ref replyMsg, stoppingToken).ConfigureAwait(false);

        if (handled)
        {
            LastReceivedTime = TimeStamp.GetMilliseconds();
            if (state.Connection.DisconnectNotifySent && state.Status is ProtocolStatus.Running)
            {
                events.Enqueue(new(ProtocolEvent.NetworkResumed));
                state.Connection.DisconnectNotifySent = false;
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
            if (state.Status is not ProtocolStatus.Disconnected && !state.Connection.DisconnectEventSent)
            {
                Tracer.Log("Disconnecting endpoint on remote request.\n");
                events.Enqueue(new(ProtocolEvent.Disconnected));
                state.Connection.DisconnectEventSent = true;
            }
        }
        else
        {
            /*
             * Update the peer connection status if this peer is still considered to be part
             * of the network.
             */
            var remoteStatus = msg.Input.PeerConnectStatus;
            var peerConnectStatus = state.PeerConnectStatus;
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
            compressor.Decompress(ref msg.Input, ref lastReceivedInput, onParsedInputCache);
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
        ProtocolEventData evt = new(ProtocolEvent.Input)
        {
            Input = lastReceivedInput,
        };


        state.Running.LastInputPacketRecvTime = (uint)TimeStamp.GetMilliseconds();

        Tracer.Log("Sending frame {0} to emu queue {1} ({2}).\n",
            lastReceivedInput.Frame,
            state.QueueIndex,
            lastAckedInput.Buffer.ToString()
        );

        events.Enqueue(evt);
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
        state.Stats.RoundTripTime = (int)(TimeStamp.GetMilliseconds() - msg.QualityReply.Pong);
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
        state.Fairness.RemoteFrameAdvantage = msg.QualityReport.FrameAdvantage;

        return true;
    }

    bool OnSyncReply(ProtocolMessage msg, ref ProtocolMessage replyMsg, out bool sendReply)
    {
        sendReply = false;
        if (state.Status is not ProtocolStatus.Syncing)
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

        if (!state.Connection.IsConnected)
        {
            events.Enqueue(new(ProtocolEvent.Connected));
            state.Connection.IsConnected = true;
        }

        Tracer.Log("Checking sync state ({0} round trips remaining).\n",
            state.Sync.RemainingRoundtrips);

        if (--state.Sync.RemainingRoundtrips == 0)
        {
            Tracer.Log("Synchronized!\n");
            events.Enqueue(new(ProtocolEvent.Synchronized));
            state.Status = ProtocolStatus.Running;
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

            events.Enqueue(evt);
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

    public void SendInputAck(out ProtocolMessage msg) =>
        msg = new(MsgType.InputAck)
        {
            InputAck = new()
            {
                AckFrame = LastReceivedInput.Frame,
            },
        };
}
