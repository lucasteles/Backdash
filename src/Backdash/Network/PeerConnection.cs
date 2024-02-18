using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol;
using Backdash.Network.Protocol.Messaging;
using Backdash.Sync;

namespace Backdash.Network;

sealed class PeerConnection<TInput>(
    ProtocolOptions options,
    ProtocolState state,
    Logger logger,
    IClock clock,
    ITimeSync<TInput> timeSync,
    IProtocolEventQueue<TInput> eventQueue,
    IProtocolSyncManager syncRequest,
    IProtocolInbox<TInput> inbox,
    IProtocolOutbox outbox,
    IProtocolInputBuffer<TInput> inputBuffer
) : IDisposable
    where TInput : struct
{
    public void Dispose() => Disconnect(true);

    public AddInputResult SendInput(in GameInput<TInput> input) => inputBuffer.SendInput(in input);

    public bool IsRunning => state.CurrentStatus is ProtocolStatus.Running;

    public PlayerHandle Player => state.Player;

    public void Disconnect(bool force = false)
    {
        state.CurrentStatus = ProtocolStatus.Disconnected;
        var shutdownTime = force ? TimeSpan.Zero : options.ShutdownTime;
        state.StoppingTokenSource.CancelAfter(shutdownTime);
        eventQueue.Dispose();
        outbox.Dispose();
    }

    // require idle input should be a configuration parameter
    public int RecommendFrameDelay => timeSync.RecommendFrameWaitDuration(false);

    public void SetLocalFrameNumber(Frame localFrame)
    {
        /*
         * Estimate which frame the other guy is one by looking at the
         * last frame they gave us plus some delta for the one-way packet
         * trip time.
         */
        var deltaFrame = (int)(state.Stats.RoundTripTime.TotalMilliseconds * 60f / 1000f);
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
        ProtocolMessage msg = new(MsgType.InputAck)
        {
            InputAck = new()
            {
                AckFrame = inbox.LastReceivedInput.Frame,
            },
        };

        return outbox.SendMessageAsync(in msg, ct);
    }

    public void GetNetworkStats(ref RollbackSessionInfo info)
    {
        var stats = state.Stats;
        info.Ping = stats.RoundTripTime;
        info.BytesSent = stats.BytesSent;
        info.PacketsSent = stats.PacketsSent;
        info.LastSendTime = clock.GetElapsedTime(stats.LastSendTime);
        info.Pps = stats.Pps;
        info.UdpOverhead = stats.UdpOverhead;
        info.BandwidthKbps = stats.BandwidthKbps;
        info.TotalBytesSent = stats.TotalBytesSent;
        info.LastSendFrame = inputBuffer.LastSent.Frame.Number;
        info.PendingInputCount = inputBuffer.PendingNumber;
        info.LastReceivedTime = clock.GetElapsedTime(inbox.LastReceivedTime);
        info.LastAckedFrame = inbox.LastAckedFrame.Number;
        info.RemoteFrameBehind = state.Fairness.RemoteFrameAdvantage.Number;
        info.LocalFrameBehind = state.Fairness.LocalFrameAdvantage.Number;
    }

    public bool GetPeerConnectStatus(int id, out Frame frame)
    {
        frame = state.PeerConnectStatuses[id].LastFrame;
        return !state.PeerConnectStatuses[id].Disconnected;
    }

    public IUdpObserver<ProtocolMessage> GetUdpObserver() => inbox;

    public void Synchronize() => syncRequest.BeginSynchronization();

    public void Update()
    {
        networkStatsTimer.Update();
        qualityReportTimer.Update();
        // keepAliveTimer.Update();
        // disconnectTimer.Update();
        // resendInputTimer.Update();
    }

    // --------------
    // Timers
    // --------------

    readonly ManualTimer qualityReportTimer = new(clock, options.QualityReportInterval, _ =>
    {
        if (state.CurrentStatus is not ProtocolStatus.Running)
            return;

        outbox
            .SendMessage(new ProtocolMessage(MsgType.QualityReport)
            {
                QualityReport = new()
                {
                    Ping = clock.GetTimeStamp(),
                    FrameAdvantage = state.Fairness.LocalFrameAdvantage.Number,
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

        var stats = state.Stats;
        var udpHeaderSent = (ByteSize)(totalHeaderSize * stats.PacketsSent);
        var seconds = elapsed.TotalSeconds;

        stats.TotalBytesSent = stats.BytesSent + udpHeaderSent;

        var bps = stats.TotalBytesSent / seconds;
        stats.Pps = (float)(stats.PacketsSent * 1000f / elapsed.TotalMilliseconds);
        stats.BandwidthKbps = (float)bps.KibiBytes;
        stats.UdpOverhead =
            (float)(100.0 * (totalHeaderSize * stats.PacketsSent) / stats.BytesSent.ByteCount);


        if (options.LogNetworkStats)
            logger.Write(LogLevel.Information,
                $"Network Stats -- Bandwidth: {stats.BandwidthKbps:f2} KBps; "
                + $"Packets Sent: {stats.PacketsSent} ({stats.Pps:f2} pps); "
                + $"KB Sent: {stats.TotalBytesSent.KibiBytes:f2}; UDP Overhead: {stats.UdpOverhead}"
            );
    });

    readonly ManualTimer keepAliveTimer = new(clock, options.KeepAliveInterval, _ =>
    {
        if (state.CurrentStatus is not ProtocolStatus.Running)
            return;

        var lastSend = state.Stats.LastSendTime;
        if (lastSend is not 0
            && clock.GetElapsedTime(lastSend) < options.KeepAliveInterval)
            return;

        logger.Write(LogLevel.Debug, "Sending keep alive packet");
        outbox.SendMessage(new ProtocolMessage(MsgType.KeepAlive)
        {
            KeepAlive = new(),
        });
    });

    readonly ManualTimer disconnectTimer = new(clock, options.DisconnectTimeout, _ =>
    {
        if (state.CurrentStatus is not ProtocolStatus.Running)
            return;

        var elapsed = clock.GetElapsedTime(inbox.LastReceivedTime);
        if (elapsed < options.DisconnectTimeout + options.DisconnectNotifyStart || state.Connection.DisconnectEventSent)
            return;

        logger.Write(LogLevel.Warning,
            $"Endpoint has stopped receiving packets for {(int)elapsed.TotalMilliseconds}ms. Disconnecting.");

        eventQueue.Publish(ProtocolEventType.Disconnected, state.Player);
        state.Connection.DisconnectEventSent = true;
    });

    readonly ManualTimer resendInputTimer = new(clock, options.ResendInputInterval, _ =>
    {
        if (state.CurrentStatus is not ProtocolStatus.Running)
            return;

        if (state.Stats.LastInputPacketRecvTime is not 0
            && clock.GetElapsedTime(state.Stats.LastInputPacketRecvTime) < options.ResendInputInterval)
            return;

        logger.Write(LogLevel.Information,
            $"Haven't exchanged packets in a while (last received:{inbox.LastReceivedInput.Frame.Number} last sent:{inputBuffer.LastSent.Frame.Number}). Resending");

        inputBuffer.SendPendingInputs();
    });
}
