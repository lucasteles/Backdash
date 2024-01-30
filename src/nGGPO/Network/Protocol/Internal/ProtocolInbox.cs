using System.Net;
using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network.Client;
using nGGPO.Network.Messages;
using nGGPO.Utils;

namespace nGGPO.Network.Protocol.Internal;

using static ProtocolConstants;

sealed class ProtocolInbox(
    ProtocolState state,
    ProtocolEventDispatcher events,
    IMessageSender messageSender,
    Random random,
    ProtocolLogger logger
) : IUdpObserver<ProtocolMessage>
{
    ushort remoteMagicNumber;
    ushort nextRecvSeq;

    GameInput lastReceivedInput = GameInput.Empty;
    public long LastReceivedTime { get; private set; }

    Frame lastAckedFrame = Frame.Null;

    public GameInput LastReceivedInput => lastReceivedInput;
    public Frame LastAckedFrame => lastAckedFrame;

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

        if (HandleMessage(ref message, out var replyMsg))
        {
            if (replyMsg.Header.Type is not MsgType.Invalid)
                await messageSender.SendMessage(ref replyMsg, stoppingToken).ConfigureAwait(false);

            LastReceivedTime = TimeStamp.GetMilliseconds();
            if (state.Connection.DisconnectNotifySent && state.Status is ProtocolStatus.Running)
            {
                events.Enqueue(ProtocolEvent.NetworkResumed);
                state.Connection.DisconnectNotifySent = false;
            }
        }
    }

    bool HandleMessage(ref ProtocolMessage message, out ProtocolMessage replyMsg)
    {
        var handled = false;
        replyMsg = new(MsgType.Invalid);

        switch (message.Header.Type)
        {
            case MsgType.SyncRequest:
                handled = OnSyncRequest(in message, ref replyMsg);
                break;
            case MsgType.SyncReply:
                handled = OnSyncReply(in message, ref replyMsg);
                break;
            case MsgType.Input:
                handled = OnInput(ref message);
                break;
            case MsgType.QualityReport:
                handled = OnQualityReport(in message, out replyMsg);
                break;
            case MsgType.QualityReply:
                handled = OnQualityReply(in message);
                break;
            case MsgType.InputAck:
                handled = OnInputAck(in message);
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

        return handled;
    }

    bool OnInput(ref ProtocolMessage msg)
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
                events.Enqueue(ProtocolEvent.Disconnected);
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
            var decompressor = InputEncoder.Decompress(ref msg.Input, ref lastReceivedInput);
            while (decompressor.NextInput())
                OnParsedInput();
        }

        Tracer.Assert(lastReceivedInput.Frame >= lastAckedFrame);

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
            lastAckedFrame
        );

        events.Enqueue(evt);
    }

    bool OnInputAck(in ProtocolMessage msg)
    {
        lastAckedFrame = new Frame(msg.InputAck.AckFrame);
        return true;
    }

    bool OnQualityReply(in ProtocolMessage msg)
    {
        state.Stats.RoundTripTime = (int)(TimeStamp.GetMilliseconds() - msg.QualityReply.Pong);
        return true;
    }

    bool OnQualityReport(in ProtocolMessage msg, out ProtocolMessage newMsg)
    {
        newMsg = new(MsgType.QualityReply)
        {
            QualityReply = new()
            {
                Pong = msg.QualityReport.Ping,
            },
        };

        state.Fairness.RemoteFrameAdvantage = msg.QualityReport.FrameAdvantage;

        return true;
    }

    bool OnSyncReply(in ProtocolMessage msg, ref ProtocolMessage replyMsg)
    {
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
            events.Enqueue(ProtocolEvent.Connected);
            state.Connection.IsConnected = true;
        }

        Tracer.Log("Checking sync state ({0} round trips remaining).\n",
            state.Sync.RemainingRoundtrips);

        if (--state.Sync.RemainingRoundtrips == 0)
        {
            Tracer.Log("Synchronized!\n");
            events.Enqueue(ProtocolEvent.Synchronized);
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

            state.Sync.Random = random.NextUInt();
            replyMsg = new(MsgType.SyncRequest)
            {
                SyncRequest = new()
                {
                    RandomRequest = state.Sync.Random,
                },
            };
        }

        return true;
    }

    public bool OnSyncRequest(in ProtocolMessage msg, ref ProtocolMessage replyMsg)
    {
        if (remoteMagicNumber is not 0 && msg.Header.Magic != remoteMagicNumber)
        {
            Tracer.Log("Ignoring sync request from unknown endpoint ({0} != {1}).\n",
                msg.Header.Magic, remoteMagicNumber);
            return false;
        }

        replyMsg = new(MsgType.SyncReply)
        {
            SyncReply = new()
            {
                RandomReply = msg.SyncRequest.RandomRequest,
            },
        };

        return true;
    }
}
