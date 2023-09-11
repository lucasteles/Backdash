using System.Net;
using Backdash.Core;
using Backdash.Data;
using Backdash.Input;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol.Events;

namespace Backdash.Network.Protocol.Messaging;

interface IProtocolInbox : IUdpObserver<ProtocolMessage>
{
    GameInput LastReceivedInput { get; }
    long LastReceivedTime { get; }
    Frame LastAckedFrame { get; }
}

sealed class ProtocolInbox(
    ProtocolOptions options,
    ProtocolState state,
    IRandomNumberGenerator random,
    IClock clock,
    IMessageSender messageSender,
    IInputEncoder inputEncoder,
    IProtocolEventDispatcher events,
    IProtocolLogger protocolLogger,
    ILogger logger
) : IProtocolInbox
{
    ushort remoteMagicNumber;
    ushort nextRecvSeq;
    GameInput lastReceivedInput = GameInput.CreateEmpty();
    public long LastReceivedTime { get; private set; }
    Frame lastAckedFrame = Frame.Null;
    public GameInput LastReceivedInput => lastReceivedInput;
    public Frame LastAckedFrame => lastAckedFrame;

    public async ValueTask OnUdpMessage(
        IUdpClient<ProtocolMessage> sender,
        ProtocolMessage message,
        SocketAddress from,
        CancellationToken stoppingToken)
    {
        if (!from.Equals(options.Peer.Address))
            return;

        var seqNum = message.Header.SequenceNumber;
        if (message.Header.Type is not MsgType.SyncRequest and not MsgType.SyncReply)
        {
            if (message.Header.Magic != remoteMagicNumber)
            {
                protocolLogger.LogMsg("recv rejecting", in message);
                return;
            }

            var skipped = (ushort)(seqNum - nextRecvSeq);
            if (skipped > options.MaxSeqDistance)
            {
                logger.Info($"dropping out of order packet (seq: {seqNum}, last seq:{nextRecvSeq})");
                return;
            }
        }

        nextRecvSeq = seqNum;
        protocolLogger.LogMsg("recv", message);

        if (HandleMessage(ref message, out var replyMsg))
        {
            if (replyMsg.Header.Type is not MsgType.Invalid)
                await messageSender.SendMessage(ref replyMsg, stoppingToken).ConfigureAwait(false);

            LastReceivedTime = clock.GetMilliseconds();
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
                logger.Error($"Invalid UdpProtocol message");
                break;
            default:
                throw new BackdashException($"Unknown UdpMsg type: {message.Header.Type}");
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
                logger.Info($"Disconnecting endpoint on remote request");
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
            var peerConnectStatus = state.PeerConnectStatuses;
            for (var i = 0; i < peerConnectStatus.Length; i++)
            {
                Tracer.Assert(remoteStatus[i].LastFrame >= peerConnectStatus[i].LastFrame);
                peerConnectStatus[i].Disconnected =
                    peerConnectStatus[i].Disconnected
                    || remoteStatus[i].Disconnected;

                peerConnectStatus[i].LastFrame = Frame.Max(
                    in peerConnectStatus[i].LastFrame,
                    in remoteStatus[i].LastFrame
                );
            }
        }

        // /*
        //  * Decompress the input.
        //  */
        if (msg.Input.InputSize > 0)
        {
            var decompressor = inputEncoder.Decompress(ref msg.Input, ref lastReceivedInput);
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


        state.Running.LastInputPacketRecvTime = (uint)clock.GetMilliseconds();

        logger.Info($"Sending frame {lastReceivedInput.Frame} to emu queue {options.Queue} (frame: {lastAckedFrame})");
        events.Enqueue(evt);
    }

    bool OnInputAck(in ProtocolMessage msg)
    {
        lastAckedFrame = msg.InputAck.AckFrame;
        return true;
    }

    bool OnQualityReply(in ProtocolMessage msg)
    {
        state.Metrics.RoundTripTime = (int)(clock.GetMilliseconds() - msg.QualityReply.Pong);
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

        state.Fairness.RemoteFrameAdvantage = new(msg.QualityReport.FrameAdvantage);

        return true;
    }

    bool OnSyncReply(in ProtocolMessage msg, ref ProtocolMessage replyMsg)
    {
        if (state.Status is not ProtocolStatus.Syncing)
        {
            logger.Info($"Ignoring SyncReply while not syncing");
            return msg.Header.Magic == remoteMagicNumber;
        }

        if (msg.SyncReply.RandomReply != state.Sync.Random)
        {
            logger.Info($"Sync reply {msg.SyncReply.RandomReply} != {state.Sync.Random}.  Keep looking...");
            return false;
        }

        if (!state.Connection.IsConnected)
        {
            events.Enqueue(ProtocolEvent.Connected);
            state.Connection.IsConnected = true;
        }

        logger.Info($"Checking sync state ({state.Sync.RemainingRoundtrips} round trips remaining)");

        if (--state.Sync.RemainingRoundtrips == 0)
        {
            logger.Info($"Synchronized!");
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
                    Total = (ushort)options.NumberOfSyncPackets,
                    Count = (ushort)(options.NumberOfSyncPackets - state.Sync.RemainingRoundtrips),
                },
            };

            events.Enqueue(evt);
            state.Sync.CreateSyncMessage(random.SyncNumber(), out replyMsg);
        }

        return true;
    }

    public bool OnSyncRequest(in ProtocolMessage msg, ref ProtocolMessage replyMsg)
    {
        if (remoteMagicNumber is not 0 && msg.Header.Magic != remoteMagicNumber)
        {
            logger.Warn($"Ignoring sync request from unknown endpoint ({msg.Header.Magic} != {remoteMagicNumber})");
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
