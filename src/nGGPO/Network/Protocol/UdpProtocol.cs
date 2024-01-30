using System.Net;
using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network.Client;
using nGGPO.Network.Messages;
using nGGPO.Network.Protocol.Internal;
using nGGPO.Utils;

namespace nGGPO.Network.Protocol;

using static ProtocolConstants;

sealed class UdpProtocol : IUdpObserver<ProtocolMessage>, IDisposable
{
    readonly ProtocolState state;

    /*
     * Packet loss...
     */
    public long ShutdownTimeout { get; set; }
    public int DisconnectTimeout { get; set; }
    public int DisconnectNotifyStart { get; set; }

    /*
     * Rift synchronization.
     */
    readonly TimeSync timeSync;


    // services
    readonly ProtocolInbox inbox;
    readonly ProtocolOutbox outbox;
    readonly ProtocolInputProcessor inputProcessor;

    public UdpProtocol(TimeSync timeSync,
        Random random,
        UdpClient<ProtocolMessage> udp,
        QueueIndex queue,
        IPEndPoint peerAddress,
        Connections localConnections,
        int networkDelay = 0
    )
    {
        this.timeSync = timeSync;

        state = new(peerAddress, localConnections)
        {
            QueueIndex = queue,
        };

        ProtocolLogger logger = new();
        ProtocolEventDispatcher eventDispatcher = new(logger);
        outbox = new(state.PeerAddress, udp, logger, random)
        {
            SendLatency = networkDelay,
        };
        inbox = new(state, eventDispatcher, outbox, random, logger);
        inputProcessor = new(this.timeSync, localConnections, outbox, inbox, state);
    }

    public void Dispose()
    {
        Disconnect();
        outbox.Dispose();
    }

    public ValueTask SendInput(in GameInput input, CancellationToken ct) =>
        inputProcessor.SendInput(input, ct);

    public void Disconnect()
    {
        state.Status = ProtocolStatus.Disconnected;
        ShutdownTimeout = TimeStamp.GetMilliseconds() + UdpShutdownTimer;
    }

    public ValueTask OnUdpMessage(
        UdpClient<ProtocolMessage> sender,
        ProtocolMessage message,
        SocketAddress from,
        CancellationToken stoppingToken
    ) => inbox.OnUdpMessage(sender, message, from, stoppingToken);

    public Task Start(CancellationToken ct) =>
        Task.WhenAll(
            outbox.Start(ct),
            inputProcessor.Start(ct)
        );

    // require idle input should be a configuration parameter
    public int RecommendFrameDelay() =>
        timeSync.RecommendFrameWaitDuration(false);

    void SetLocalFrameNumber(Frame localFrame)
    {
        /*
         * Estimate which frame the other guy is one by looking at the
         * last frame they gave us plus some delta for the one-way packet
         * trip time.
         */
        var remoteFrame = inbox.LastReceivedInput.Frame + (state.Stats.RoundTripTime * 60 / 1000);

        /*
         * Our frame advantage is how many frames *behind* the other guy
         * we are.  Counter-intuitive, I know.  It's an advantage because
         * it means they'll have to predict more often and our moves will
         * pop more frequently.
         */
        state.Fairness.LocalFrameAdvantage = remoteFrame - localFrame;
    }

    public void GetNetworkStats(ref NetworkStats stats)
    {
        stats.Ping = state.Stats.RoundTripTime;
        stats.SendQueueLen = inputProcessor.PendingNumber;
        stats.RemoteFramesBehind = state.Fairness.RemoteFrameAdvantage;
        stats.LocalFramesBehind = state.Fairness.LocalFrameAdvantage;
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

        return outbox.SendMessage(ref msg, ct);
    }

    public void Synchronize()
    {
        state.Status = ProtocolStatus.Syncing;
        throw new NotImplementedException();
    }

    public bool IsInitialized()
    {
        throw new NotImplementedException();
    }
}
