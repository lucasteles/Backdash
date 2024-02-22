using System.Diagnostics;
using System.Net;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Serialization;
using Backdash.Sync;

namespace Backdash.Network.Protocol.Messaging;

interface IProtocolInbox<TInput> : IUdpObserver<ProtocolMessage> where TInput : struct
{
    GameInput<TInput> LastReceivedInput { get; }
    long LastReceivedTime { get; }
    Frame LastAckedFrame { get; }
}

sealed class ProtocolInbox<TInput>(
    ProtocolOptions options,
    IBinaryReader<TInput> inputSerializer,
    ProtocolState state,
    IClock clock,
    IProtocolSynchronizer sync,
    IMessageSender messageSender,
    IProtocolEventQueue<TInput> events,
    Logger logger
) : IProtocolInbox<TInput> where TInput : struct
{
    ushort remoteMagicNumber;
    ushort nextRecvSeq;
    GameInput<TInput> lastReceivedInput = new();
    readonly Memory<byte> lastReceivedInputBuffer = Mem.CreatePinnedMemory(Max.CompressedBytes);

    public long LastReceivedTime { get; private set; }
    public GameInput<TInput> LastReceivedInput => lastReceivedInput;
    public Frame LastAckedFrame { get; private set; } = Frame.Null;

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
        if (message.Header.Type is not MessageType.SyncRequest and not MessageType.SyncReply)
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
                logger.Write(LogLevel.Debug, $"dropping out of order packet (seq: {seqNum}, last seq:{nextRecvSeq})");
                return;
            }
        }

        nextRecvSeq = seqNum;
        logger.Write(LogLevel.Trace, $"recv {message}");

        if (HandleMessage(ref message, out var replyMsg))
        {
            if (replyMsg.Header.Type is not MessageType.Invalid)
                await messageSender.SendMessageAsync(in replyMsg, stoppingToken).ConfigureAwait(false);

            LastReceivedTime = clock.GetTimeStamp();
            if (state.Connection.DisconnectNotifySent && state.CurrentStatus is ProtocolStatus.Running)
            {
                events.Publish(ProtocolEvent.NetworkResumed, state.Player);
                state.Connection.DisconnectNotifySent = false;
            }
        }
    }

    bool HandleMessage(ref ProtocolMessage message, out ProtocolMessage replyMsg)
    {
        var handled = false;
        replyMsg = new(MessageType.Invalid);

        switch (message.Header.Type)
        {
            case MessageType.SyncRequest:
                handled = OnSyncRequest(in message, ref replyMsg);
                break;
            case MessageType.SyncReply:
                handled = OnSyncReply(in message, ref replyMsg);
                break;
            case MessageType.Input:
                handled = OnInput(ref message.Input);
                break;
            case MessageType.QualityReport:
                handled = OnQualityReport(in message, out replyMsg);
                break;
            case MessageType.QualityReply:
                handled = OnQualityReply(in message);
                break;
            case MessageType.InputAck:
                handled = OnInputAck(in message);
                break;
            case MessageType.KeepAlive:
                handled = true;
                break;
            case MessageType.Invalid:
                logger.Write(LogLevel.Error, "Invalid UdpProtocol message");
                break;
            default:
                throw new BackdashException($"Unknown UdpMsg type: {message.Header.Type}");
        }

        return handled;
    }

    bool OnInput(ref InputMessage msg)
    {
        LastAckedFrame = msg.AckFrame;
        logger.Write(LogLevel.Trace, $"Acked Frame: {LastAckedFrame}");

        /*
         * If a disconnect is requested, go ahead and disconnect now.
         */
        var disconnectRequested = msg.DisconnectRequested;

        if (disconnectRequested)
        {
            if (state.CurrentStatus is not ProtocolStatus.Disconnected && !state.Connection.DisconnectEventSent)
            {
                logger.Write(LogLevel.Information, "Disconnecting endpoint on remote request");
                events.Publish(ProtocolEvent.Disconnected, state.Player);
                state.Connection.DisconnectEventSent = true;
            }
        }
        else
        {
            /*
             * Update the peer connection status if this peer is still considered to be part
             * of the network.
             */
            var remoteStatus = msg.PeerConnectStatus;
            var peerConnectStatus = state.PeerConnectStatuses;
            for (var i = 0; i < peerConnectStatus.Length; i++)
            {
                Trace.Assert(remoteStatus[i].LastFrame >= peerConnectStatus[i].LastFrame);
                peerConnectStatus[i].Disconnected = peerConnectStatus[i].Disconnected || remoteStatus[i].Disconnected;
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
        if (msg.NumBits > 0)
        {
            if (lastReceivedInput.Frame < 0)
                lastReceivedInput.Frame = msg.StartFrame.Previous();

            var nextFrame = lastReceivedInput.Frame.Next();
            var currentFrame = msg.StartFrame;
            Trace.Assert(currentFrame <= nextFrame);

            var decompressor = InputEncoder.GetDecompressor(ref msg);

            var framesAhead = nextFrame.Number - currentFrame.Number;
            if (framesAhead > 0)
            {
                logger.Write(LogLevel.Trace,
                    $"Skipping past {framesAhead} frames (current: {currentFrame}, last: {lastReceivedInput.Frame})");

                if (decompressor.Skip(framesAhead))
                    currentFrame += framesAhead;
            }

            Trace.Assert(currentFrame == nextFrame);

            var lastReceivedBuffer = lastReceivedInputBuffer.Span[..msg.InputSize];
            while (decompressor.Read(lastReceivedBuffer))
            {
                inputSerializer.Deserialize(lastReceivedBuffer, ref lastReceivedInput.Data);
                Trace.Assert(currentFrame == lastReceivedInput.Frame.Next());
                lastReceivedInput.Frame = currentFrame;
                currentFrame++;

                state.Stats.LastInputPacketRecvTime = clock.GetTimeStamp();

                logger.Write(LogLevel.Debug,
                    $"Received input: frame {lastReceivedInput.Frame}, sending to emulator queue {state.Player} (ack: {LastAckedFrame})");

                events.Publish(
                    new ProtocolEventInfo<TInput>(ProtocolEvent.Input, state.Player)
                    {
                        Input = lastReceivedInput,
                    }
                );
            }
        }

        Trace.Assert(lastReceivedInput.Frame >= lastReceivedFrame);
        return true;
    }

    bool OnInputAck(in ProtocolMessage msg)
    {
        LastAckedFrame = msg.InputAck.AckFrame;
        return true;
    }

    bool OnQualityReply(in ProtocolMessage msg)
    {
        state.Stats.RoundTripTime = clock.GetElapsedTime(msg.QualityReply.Pong);
        return true;
    }

    bool OnQualityReport(in ProtocolMessage msg, out ProtocolMessage newMsg)
    {
        newMsg = new(MessageType.QualityReply)
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
        var elapsed = clock.GetElapsedTime(msg.SyncReply.Pong);

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
            events.Publish(ProtocolEvent.Connected, state.Player);
            state.Connection.IsConnected = true;
        }

        logger.Write(LogLevel.Debug,
            $"Checking sync state ({state.Sync.RemainingRoundtrips} round trips remaining)");

        if (options.NumberOfSyncPackets >= state.Sync.RemainingRoundtrips)
            state.Sync.TotalRoundtripsPing = TimeSpan.Zero;

        state.Sync.TotalRoundtripsPing += elapsed;

        if (--state.Sync.RemainingRoundtrips == 0)
        {
            var ping = state.Sync.TotalRoundtripsPing / options.NumberOfSyncPackets;
            logger.Write(LogLevel.Information,
                $"Player {state.Player.Number} Synchronized! (Ping: {ping.TotalMilliseconds:f4})");
            state.CurrentStatus = ProtocolStatus.Running;
            state.Stats.RoundTripTime = ping;
            lastReceivedInput.ResetFrame();
            remoteMagicNumber = msg.Header.Magic;
            events.Publish(new(ProtocolEvent.Synchronized, state.Player)
            {
                Synchronized = new(ping),
            });
        }
        else
        {
            events.Publish(
                new ProtocolEventInfo<TInput>(ProtocolEvent.Synchronizing, state.Player)
                {
                    Synchronizing = new(
                        TotalSteps: options.NumberOfSyncPackets,
                        CurrentStep: options.NumberOfSyncPackets - state.Sync.RemainingRoundtrips
                    ),
                }
            );

            sync.CreateRequestMessage(out replyMsg);
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
        return true;
    }
}
