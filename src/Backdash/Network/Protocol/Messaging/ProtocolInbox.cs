using System.Diagnostics;
using System.Net;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Sync;

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
    IClock clock,
    IProtocolSyncManager sync,
    IMessageSender messageSender,
    IProtocolEventQueue events,
    Logger logger
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
        CancellationToken stoppingToken
    )
    {
        if (!from.Equals(state.Peer.Address))
            return;

        var seqNum = message.Header.SequenceNumber;
        if (message.Header.Type is not MsgType.SyncRequest and not MsgType.SyncReply)
        {
            if (state.CurrentStatus is not ProtocolStatus.Running)
            {
                logger.Write(LogLevel.Debug, $"recv skip (not ready): {message}");
                return;
            }

            if (message.Header.Magic != remoteMagicNumber)
            {
                logger.Write(LogLevel.Debug, $"recv rejecting: {message}");
                return;
            }

            var skipped = (ushort)(seqNum - nextRecvSeq);
            if (skipped > options.MaxSeqDistance)
            {
                logger.Write(LogLevel.Debug,
                    $"dropping out of order packet (seq: {seqNum}, last seq:{nextRecvSeq})");
                return;
            }
        }

        nextRecvSeq = seqNum;
        logger.Write(LogLevel.Trace, $"recv: {message}");

        if (HandleMessage(ref message, out var replyMsg))
        {
            if (replyMsg.Header.Type is not MsgType.Invalid)
                await messageSender.SendMessageAsync(in replyMsg, stoppingToken).ConfigureAwait(false);

            LastReceivedTime = clock.GetTimeStamp();
            if (state.Connection.DisconnectNotifySent && state.CurrentStatus is ProtocolStatus.Running)
            {
                events.Publish(ProtocolEventType.NetworkResumed, state.Player);
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
                logger.Write(LogLevel.Error, "Invalid UdpProtocol message");
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
            if (state.CurrentStatus is not ProtocolStatus.Disconnected && !state.Connection.DisconnectEventSent)
            {
                logger.Write(LogLevel.Information, "Disconnecting endpoint on remote request");
                events.Publish(ProtocolEventType.Disconnected, state.Player);
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
                Trace.Assert(remoteStatus[i].LastFrame >= peerConnectStatus[i].LastFrame);
                peerConnectStatus[i].Disconnected =
                    peerConnectStatus[i].Disconnected
                    || remoteStatus[i].Disconnected;

                peerConnectStatus[i].LastFrame = Frame.Max(
                    in peerConnectStatus[i].LastFrame,
                    in remoteStatus[i].LastFrame
                );
            }
        }

        /*
         * Decompress the input.
         */
        var lastReceivedFrame = lastReceivedInput.Frame;
        if (msg.Input.NumBits > 0)
        {
            lastReceivedInput.Size = msg.Input.InputSize;
            if (lastReceivedInput.Frame < 0)
                lastReceivedInput.Frame = msg.Input.StartFrame.Previous();

            var nextFrame = lastReceivedInput.Frame.Next();
            var currentFrame = msg.Input.StartFrame;
            Trace.Assert(currentFrame <= nextFrame);

            var decompressor = InputEncoder.GetDecompressor(ref msg.Input);

            var framesAhead = nextFrame.Number - currentFrame.Number;
            if (framesAhead > 0)
            {
                logger.Write(LogLevel.Trace,
                    $"Skipping past {framesAhead} frames (current: {currentFrame}, last: {lastReceivedInput.Frame})");

                if (decompressor.Skip(framesAhead))
                    currentFrame += framesAhead;
            }

            Trace.Assert(currentFrame == nextFrame);

            while (decompressor.Read(lastReceivedInput.Buffer))
            {
                /*
                 * Move forward 1 frame in the stream.
                 */
                Trace.Assert(currentFrame == lastReceivedInput.Frame.Next());
                lastReceivedInput.Frame = currentFrame;
                currentFrame++;

                state.Stats.LastInputPacketRecvTime = clock.GetTimeStamp();

                /*
                 * Send the event to the emulator
                 */
                logger.Write(LogLevel.Debug,
                    $"Sending frame {lastReceivedInput.Frame} to emulator queue {state.Player} (frame: {lastAckedFrame})");

                events.Publish(
                    new ProtocolEvent(ProtocolEventType.Input, state.Player)
                    {
                        Input = lastReceivedInput,
                    }
                );
            }
        }

        Trace.Assert(lastReceivedInput.Frame >= lastReceivedFrame);
        lastAckedFrame = msg.Input.AckFrame;
        logger.Write(LogLevel.Trace, $"Acked Frame: {LastAckedFrame}");
        return true;
    }

    bool OnInputAck(in ProtocolMessage msg)
    {
        lastAckedFrame = msg.InputAck.AckFrame;
        return true;
    }

    bool OnQualityReply(in ProtocolMessage msg)
    {
        state.Stats.RoundTripTime = clock.GetElapsedTime(msg.QualityReply.Pong);
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
        if (state.CurrentStatus is not ProtocolStatus.Syncing)
        {
            logger.Write(LogLevel.Trace, "Ignoring SyncReply while not syncing");
            return msg.Header.Magic == remoteMagicNumber;
        }

        if (msg.SyncReply.RandomReply != state.Sync.CurrentRandom)
        {
            logger.Write(LogLevel.Debug,
                $"Sync reply {msg.SyncReply.RandomReply} != {state.Sync.CurrentRandom}. Keep looking.");
            return false;
        }

        if (!state.Connection.IsConnected)
        {
            events.Publish(ProtocolEventType.Connected, state.Player);
            state.Connection.IsConnected = true;
        }

        logger.Write(LogLevel.Debug,
            $"Checking sync state ({state.Sync.RemainingRoundtrips} round trips remaining)");

        if (--state.Sync.RemainingRoundtrips == 0)
        {
            logger.Write(LogLevel.Information, $"Player {state.Player.Number} Synchronized!");
            state.CurrentStatus = ProtocolStatus.Running;
            lastReceivedInput.ResetFrame();
            remoteMagicNumber = msg.Header.Magic;
            events.Publish(ProtocolEventType.Synchronized, state.Player);
        }
        else
        {
            events.Publish(
                new ProtocolEvent(ProtocolEventType.Synchronizing, state.Player)
                {
                    Synchronizing = new()
                    {
                        Total = (ushort)options.NumberOfSyncPackets,
                        Count = (ushort)(options.NumberOfSyncPackets - state.Sync.RemainingRoundtrips),
                    },
                }
            );

            sync.CreateRequestMessage(out replyMsg);
            logger.Write(LogLevel.Debug, $"Sync Request Reply: {state.Sync.CurrentRandom}");
        }

        return true;
    }

    public bool OnSyncRequest(in ProtocolMessage msg, ref ProtocolMessage replyMsg)
    {
        if (remoteMagicNumber is not 0 && msg.Header.Magic != remoteMagicNumber)
        {
            logger.Write(LogLevel.Warning,
                $"Ignoring sync request from unknown endpoint ({msg.Header.Magic} != {remoteMagicNumber})");
            return false;
        }

        sync.CreateReplyMessage(in msg.SyncRequest, out replyMsg);
        logger.Write(LogLevel.Debug, $"Sync Reply: {msg.SyncRequest.RandomRequest}");
        return true;
    }
}
