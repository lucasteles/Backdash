using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network.Messages;
using nGGPO.Network.Protocol.Internal;
using nGGPO.Utils;

namespace nGGPO.Network.Protocol;

using static ProtocolConstants;

sealed class UdpProtocol(
    ProtocolOptions options,
    ProtocolState state,
    IRandomNumberGenerator random,
    ITimeSync timeSync,
    IProtocolInbox inbox,
    IProtocolOutbox outbox,
    IProtocolInputProcessor inputProcessor)
    : IDisposable
{
    public long ShutdownTimeout { get; set; }

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
        var remoteFrame = inbox.LastReceivedInput.Frame + (state.Metrics.RoundTripTime * 60 / 1000);

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
        stats.Ping = state.Metrics.RoundTripTime;
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

    public async ValueTask Synchronize(CancellationToken ct)
    {
        state.Status = ProtocolStatus.Syncing;
        state.Sync.RemainingRoundtrips = (uint)options.NumberOfSyncPackets;
        state.Sync.CreateSyncMessage(random.SyncNumber(), out var syncMsg);
        await outbox.SendMessage(ref syncMsg, ct);
    }
}
