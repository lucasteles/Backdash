using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol;
using Backdash.Network.Protocol.Messaging;
using Backdash.Sync;
using Backdash.Sync.Input;

namespace Backdash.Network;

sealed class PeerConnection<TInput>(
    ProtocolOptions options,
    ProtocolState state,
    Logger logger,
    IClock clock,
    ITimeSync<TInput> timeSync,
    IProtocolEventQueue<TInput> eventQueue,
    IProtocolSynchronizer syncRequest,
    IProtocolInbox<TInput> inbox,
    IProtocolOutbox outbox,
    IProtocolInputBuffer<TInput> inputBuffer
) : IDisposable
    where TInput : struct
{
    public SendInputResult SendInput(in GameInput<TInput> input) => inputBuffer.SendInput(in input);

    public ProtocolStatus Status => state.CurrentStatus;
    public bool IsRunning => state.CurrentStatus is ProtocolStatus.Running;

    public PlayerHandle Player => state.Player;

    public void Dispose()
    {
        if (!state.StoppingTokenSource.IsCancellationRequested)
            state.StoppingTokenSource.Cancel();

        eventQueue.Dispose();
        inputBuffer.Dispose();
        outbox.Dispose();
    }

    public void Disconnect()
    {
        if (state.CurrentStatus is ProtocolStatus.Disconnected) return;
        state.CurrentStatus = ProtocolStatus.Disconnected;

        state.StoppingTokenSource.CancelAfter(options.ShutdownTime);
    }

    // require idle input should be a configuration parameter
    public int GetRecommendFrameDelay(bool requireIdleInput) => timeSync.RecommendFrameWaitDuration(requireIdleInput);

    public void SetLocalFrameNumber(Frame localFrame, short fps = FrameSpan.DefaultFramesPerSecond)
    {
        /*
         * Estimate which frame the other guy is one by looking at the
         * last frame they gave us plus some delta for the one-way packet
         * trip time.
         */
        var deltaFrame = FrameSpan.FromMilliseconds(state.Stats.RoundTripTime.TotalMilliseconds, fps);
        var remoteFrame = inbox.LastReceivedInput.Frame + deltaFrame;

        /*
         * Our frame advantage is how many frames *behind* the other guy
         * we are.  Counter-intuitive, I know.  It's an advantage because
         * it means they'll have to predict more often and our moves will
         * pop more frequently.
         */
        state.Fairness.LocalFrameAdvantage = remoteFrame - localFrame;
    }

    public ValueTask SendInputAck(CancellationToken ct)
    {
        ProtocolMessage msg = new(MessageType.InputAck)
        {
            InputAck = new()
            {
                AckFrame = inbox.LastReceivedInput.Frame,
            },
        };

        return outbox.SendMessageAsync(in msg, ct);
    }

    public void GetNetworkStats(ref RollbackNetworkStatus info)
    {
        var stats = state.Stats;
        info.Ping = stats.RoundTripTime;
        info.PendingInputCount = inputBuffer.PendingNumber;
        info.LastAckedFrame = inbox.LastAckedFrame;
        info.RemoteFramesBehind = state.Fairness.RemoteFrameAdvantage;
        info.LocalFramesBehind = state.Fairness.LocalFrameAdvantage;

        info.Send.TotalBytes = stats.Send.TotalBytesWithHeaders;
        info.Send.Count = stats.Send.TotalPackets;
        info.Send.LastTime = clock.GetElapsedTime(stats.Send.LastTime);
        info.Send.PackagesPerSecond = stats.Send.PackagesPerSecond;
        info.Send.Bandwidth = stats.Send.Bandwidth;
        info.Send.LastFrame = inputBuffer.LastSent.Frame;

        info.Received.TotalBytes = stats.Received.TotalBytesWithHeaders;
        info.Received.LastTime = clock.GetElapsedTime(stats.Received.LastTime);
        info.Received.Count = stats.Received.TotalPackets;
        info.Received.PackagesPerSecond = stats.Received.PackagesPerSecond;
        info.Received.Bandwidth = stats.Received.Bandwidth;
        info.Received.LastFrame = inbox.LastReceivedInput.Frame;
    }

    public bool GetPeerConnectStatus(int id, out Frame frame)
    {
        frame = state.PeerConnectStatuses[id].LastFrame;
        return !state.PeerConnectStatuses[id].Disconnected;
    }

    public IUdpObserver<ProtocolMessage> GetUdpObserver() => inbox;

    public void Synchronize() => syncRequest.Synchronize();

    public void Update()
    {
        if (state.CurrentStatus is not ProtocolStatus.Running)
            return;

        KeepLive();
        ResendInputs();
        qualityReportTimer.Update();
        networkStatsTimer.Update();
        CheckDisconnection();
    }

    long lastKeepAliveSent;

    public void KeepLive()
    {
        var lastSend = state.Stats.Send.LastTime;
        if (lastSend <= 0 || clock.GetElapsedTime(lastSend) <= options.KeepAliveInterval)
            return;

        if (lastKeepAliveSent <= 0 || clock.GetElapsedTime(lastKeepAliveSent) <= options.KeepAliveInterval)
            return;

        logger.Write(LogLevel.Debug, "Sending keep alive packet");
        lastKeepAliveSent = clock.GetTimeStamp();
        outbox.SendMessage(new ProtocolMessage(MessageType.KeepAlive)
        {
            KeepAlive = new(),
        });
    }

    public void ResendInputs()
    {
        var lastReceivedInputTime = state.Stats.LastReceivedInputTime;
        if (lastReceivedInputTime <= 0 || clock.GetElapsedTime(lastReceivedInputTime) <= options.ResendInputInterval)
            return;

        logger.Write(LogLevel.Debug,
            $"Haven't exchanged packets in a while (last received:{inbox.LastReceivedInput.Frame.Number} last sent:{inputBuffer.LastSent.Frame.Number}). Resending");

        inputBuffer.SendPendingInputs();
    }

    public void CheckDisconnection()
    {
        if (state.Stats.Received.LastTime <= 0 || options.DisconnectTimeout <= TimeSpan.Zero) return;
        var lastReceivedTime = clock.GetElapsedTime(state.Stats.Received.LastTime);

        if (lastReceivedTime > options.DisconnectNotifyStart
            && state.Connection is { DisconnectNotifySent: false, DisconnectEventSent: false })
        {
            state.Connection.DisconnectNotifySent = true;

            eventQueue.Publish(new(ProtocolEvent.NetworkInterrupted, state.Player)
            {
                NetworkInterrupted = new()
                {
                    DisconnectTimeout = options.DisconnectTimeout - options.DisconnectNotifyStart,
                },
            });

            logger.Write(LogLevel.Warning,
                $"Endpoint has stopped receiving packets for {(int)lastReceivedTime.TotalMilliseconds}ms. Sending notification");

            return;
        }

        if (lastReceivedTime > options.DisconnectTimeout && !state.Connection.DisconnectEventSent)
        {
            state.Connection.DisconnectEventSent = true;
            logger.Write(LogLevel.Warning,
                $"Endpoint has stopped receiving packets for {(int)lastReceivedTime.TotalMilliseconds}ms. Disconnecting");
            eventQueue.Publish(ProtocolEvent.Disconnected, state.Player);
        }
    }


    // --------------
    // Timers
    // --------------

    readonly ManualTimer qualityReportTimer = new(clock, options.QualityReportInterval, _ =>
    {
        if (state.CurrentStatus is not ProtocolStatus.Running)
            return;

        outbox
            .SendMessage(new ProtocolMessage(MessageType.QualityReport)
            {
                QualityReport = new()
                {
                    Ping = clock.GetTimeStamp(),
                    FrameAdvantage = state.Fairness.LocalFrameAdvantage.FrameCount,
                },
            });
    });

    readonly ManualTimer networkStatsTimer = new(clock, options.NetworkStatsInterval, elapsed =>
    {
        const int udpHeaderSize = 8;
        const int ipAddressHeaderSize = 20;
        const int totalHeaderSize = udpHeaderSize + ipAddressHeaderSize;

        if (state.CurrentStatus is not ProtocolStatus.Running)
            return;

        var seconds = elapsed.TotalSeconds;

        UpdateStats(ref state.Stats.Send);
        UpdateStats(ref state.Stats.Received);

        if (options.LogNetworkStats)
        {
            logger.Write(LogLevel.Information, $"Network Stats(send): {state.Stats.Send}");
            logger.Write(LogLevel.Information, $"Network Stats(recv): {state.Stats.Received}");
        }

        void UpdateStats(ref ProtocolState.PackagesStats stats)
        {
            var totalUdpHeaderSize = (ByteSize)(totalHeaderSize * stats.TotalPackets);

            stats.TotalBytesWithHeaders = stats.TotalBytes + totalUdpHeaderSize;
            stats.TotalBytesWithHeaders = stats.TotalBytes + totalUdpHeaderSize;

            stats.PackagesPerSecond = (float)(stats.TotalPackets * 1000f / elapsed.TotalMilliseconds);
            stats.Bandwidth = stats.TotalBytesWithHeaders / seconds;
            stats.UdpOverhead =
                (float)(100.0 * (totalHeaderSize * stats.TotalPackets) / stats.TotalBytes.ByteCount);
        }
    });
}
