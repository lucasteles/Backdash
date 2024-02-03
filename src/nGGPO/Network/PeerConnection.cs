using nGGPO.Core;
using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network.Client;
using nGGPO.Network.Messages;
using nGGPO.Network.Protocol;
using nGGPO.Network.Protocol.Events;
using nGGPO.Network.Protocol.Messaging;

namespace nGGPO.Network;

sealed class PeerConnection(
    ProtocolOptions options,
    ProtocolState state,
    IRandomNumberGenerator random,
    IClock clock,
    ITimeSync timeSync,
    IProtocolInbox inbox,
    IProtocolOutbox outbox,
    IProtocolInputProcessor inputProcessor
) : IDisposable
{
    public long ShutdownTimeout { get; set; }

    public void Dispose()
    {
        Disconnect();
        outbox.Dispose();
    }

    public ValueTask SendInput(in GameInput input, CancellationToken ct) => inputProcessor.SendInput(input, ct);
    public bool TrySendInput(in GameInput input) => inputProcessor.TrySendInput(in input);

    public void Disconnect()
    {
        state.Status = ProtocolStatus.Disconnected;
        ShutdownTimeout = clock.GetMilliseconds() + options.UdpShutdownTimer;
    }

    public Task Start(CancellationToken ct) =>
        Task.WhenAll(
            outbox.Start(ct),
            inputProcessor.Start(ct)
        );

    // require idle input should be a configuration parameter
    public int RecommendFrameDelay() => timeSync.RecommendFrameWaitDuration(false);

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

    public void GetNetworkStats(ref PeerConnectionInfo metrics)
    {
        metrics.Ping = state.Metrics.RoundTripTime;
        metrics.SendQueueLen = inputProcessor.PendingNumber;
        metrics.RemoteFramesBehind = state.Fairness.RemoteFrameAdvantage;
        metrics.LocalFramesBehind = state.Fairness.LocalFrameAdvantage;
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

    public IUdpObserver<ProtocolMessage> GetUdpObserver() => inbox;

    public async ValueTask Synchronize(CancellationToken ct)
    {
        state.Status = ProtocolStatus.Syncing;
        state.Sync.RemainingRoundtrips = (uint)options.NumberOfSyncPackets;
        state.Sync.CreateSyncMessage(random.SyncNumber(), out var syncMsg);
        await outbox.SendMessage(ref syncMsg, ct);
    }
}
