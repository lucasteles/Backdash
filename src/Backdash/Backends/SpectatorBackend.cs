using System.Diagnostics;
using System.Net;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol;
using Backdash.Serialization;
using Backdash.Sync.Input;
using Backdash.Sync.Input.Spectator;

namespace Backdash.Backends;

sealed class SpectatorBackend<TInput, TGameState> :
    IRollbackSession<TInput, TGameState>,
    IProtocolNetworkEventHandler,
    IProtocolInputEventPublisher<CombinedInputs<TInput>>
    where TInput : struct
    where TGameState : IEquatable<TGameState>, new()
{
    readonly Logger logger;
    readonly IUdpClient<ProtocolMessage> udp;
    readonly IPEndPoint hostEndpoint;
    readonly RollbackOptions options;
    readonly IBackgroundJobManager backgroundJobManager;
    readonly IClock clock;
    readonly ConnectionsState localConnections = new(0);

    readonly GameInput<CombinedInputs<TInput>>[] inputs;
    readonly PeerConnection<CombinedInputs<TInput>> host;
    IRollbackHandler<TGameState> callbacks;

    bool isSynchronizing;
    Task backgroundJobTask = Task.CompletedTask;
    bool disposed;
    long startTimestamp;
    long lastReceivedInputTime;

    SynchronizedInput<TInput>[] syncInputBuffer = [];

    public SpectatorBackend(
        int port,
        IPEndPoint hostEndpoint,
        RollbackOptions options,
        BackendServices<TInput, TGameState> services)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(hostEndpoint);
        ThrowHelpers.ThrowIfArgumentIsZeroOrLess(port);

        this.hostEndpoint = hostEndpoint;
        this.options = options;

        backgroundJobManager = services.JobManager;
        logger = services.Logger;
        clock = services.Clock;
        IBinarySerializer<CombinedInputs<TInput>> inputGroupSerializer =
            new CombinedInputsSerializer<TInput>(services.InputSerializer);

        UdpObserverGroup<ProtocolMessage> udpObservers = new();
        callbacks = new EmptySessionHandler<TGameState>(logger);
        inputs = new GameInput<CombinedInputs<TInput>>[options.SpectatorInputBufferLength];

        var selectedEndianness = Platform.GetEndianness(options.NetworkEndianness);
        udp = services.UdpClientFactory.CreateClient(
            port,
            selectedEndianness,
            options.Protocol.UdpPacketBufferSize,
            udpObservers,
            logger
        );
        backgroundJobManager.Register(udp);

        PeerConnectionFactory peerConnectionFactory = new(
            this, clock, services.Random, services.DelayStrategy, logger,
            backgroundJobManager, udp, options.Protocol, options.TimeSync
        );
        ProtocolState protocolState =
            new(new PlayerHandle(PlayerType.Remote, 0), hostEndpoint, localConnections, options.FramesPerSecond);
        host = peerConnectionFactory.Create(protocolState, inputGroupSerializer, this);
        udpObservers.Add(host.GetUdpObserver());
        host.Synchronize();
        isSynchronizing = true;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        Close();

        udp.Dispose();
        logger.Dispose();
        backgroundJobManager.Dispose();
    }

    public void Close()
    {
        logger.Write(LogLevel.Information, "Shutting down connections");
        host.Dispose();
        callbacks.OnSessionClose();
    }

    public Frame CurrentFrame { get; private set; } = Frame.Zero;
    public FrameSpan RollbackFrames { get; } = FrameSpan.Zero;
    public int NumberOfPlayers { get; private set; }
    public int NumberOfSpectators => 0;

    public ResultCode AddLocalInput(PlayerHandle player, TInput localInput) => ResultCode.Ok;
    public IReadOnlyCollection<PlayerHandle> GetPlayers() => [];
    public IReadOnlyCollection<PlayerHandle> GetSpectators() => [];

    public int FramesPerSecond
    {
        get
        {
            var elapsed = clock.GetElapsedTime(startTimestamp).TotalSeconds;
            return elapsed > 0 ? (int)(CurrentFrame.Number / elapsed) : 0;
        }
    }

    public void BeginFrame()
    {
        if (lastReceivedInputTime > 0
            && clock.GetElapsedTime(lastReceivedInputTime) > options.Protocol.DisconnectTimeout)
            Close();
    }

    public void AdvanceFrame() => logger.Write(LogLevel.Debug, $"[End Frame {CurrentFrame}]");

    public PlayerConnectionStatus GetPlayerStatus(in PlayerHandle player) => host.Status.ToPlayerStatus();

    public ResultCode AddPlayer(Player player) => ResultCode.NotSupported;

    public IReadOnlyList<ResultCode> AddPlayers(IReadOnlyList<Player> players) =>
        Enumerable.Repeat(ResultCode.NotSupported, players.Count).ToArray();

    public bool GetNetworkStatus(in PlayerHandle player, ref RollbackNetworkStatus info)
    {
        host.GetNetworkStats(ref info);
        return true;
    }

    public void SetFrameDelay(PlayerHandle player, int delayInFrames) { }

    public void Start(CancellationToken stoppingToken = default)
    {
        backgroundJobTask = backgroundJobManager.Start(stoppingToken);
        logger.Write(LogLevel.Information, $"Spectating started on host {hostEndpoint}");
    }

    public async Task WaitToStop(CancellationToken stoppingToken = default)
    {
        backgroundJobManager.Stop(TimeSpan.Zero);
        await backgroundJobTask.WaitAsync(stoppingToken).ConfigureAwait(false);
    }

    public void SetHandler(IRollbackHandler<TGameState> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        callbacks = handler;
    }

    public void OnNetworkEvent(in ProtocolEventInfo evt)
    {
        ref readonly var player = ref evt.Player;

        switch (evt.Type)
        {
            case ProtocolEvent.Connected:
                callbacks.OnPeerEvent(player, new(PeerEvent.Connected));
                break;
            case ProtocolEvent.Synchronizing:
                callbacks.OnPeerEvent(player, new(PeerEvent.Synchronizing)
                {
                    Synchronizing = new(evt.Synchronizing.CurrentStep, evt.Synchronizing.TotalSteps),
                });
                break;
            case ProtocolEvent.Synchronized:
                callbacks.OnPeerEvent(player, new(PeerEvent.Synchronized)
                {
                    Synchronized = new(evt.Synchronized.Ping),
                });
                callbacks.OnSessionStart();
                startTimestamp = clock.GetTimeStamp();
                isSynchronizing = false;
                break;
            case ProtocolEvent.SyncFailure:
                Close();
                break;
            case ProtocolEvent.NetworkInterrupted:
                callbacks.OnPeerEvent(player, new(PeerEvent.ConnectionInterrupted)
                {
                    ConnectionInterrupted = new(evt.NetworkInterrupted.DisconnectTimeout),
                });
                break;
            case ProtocolEvent.NetworkResumed:
                callbacks.OnPeerEvent(player, new(PeerEvent.ConnectionResumed));
                break;
            case ProtocolEvent.Disconnected:
                callbacks.OnPeerEvent(player, new(PeerEvent.Disconnected));
                break;
            default:
                logger.Write(LogLevel.Warning, $"Unknown protocol event {evt} from {player}");
                break;
        }
    }

    public ResultCode SynchronizeInputs()
    {
        if (isSynchronizing)
            return ResultCode.NotSynchronized;

        ref var input = ref inputs[CurrentFrame.Number % inputs.Length];

        if (input.Data.Count is 0 && CurrentFrame == Frame.Zero)
            return ResultCode.NotSynchronized;

        if (input.Frame < CurrentFrame)
        {
            // Haven't received the input from the host yet.  Wait
            return ResultCode.PredictionThreshold;
        }

        if (input.Frame > CurrentFrame)
        {
            // The host is way way way far ahead of the spectator.  How'd this
            // happen?  Anyway, the input we need is gone forever.
            return ResultCode.InputDropped;
        }

        Trace.Assert(input.Data.Count > 0);
        NumberOfPlayers = input.Data.Count;

        if (syncInputBuffer.Length != NumberOfPlayers)
            Array.Resize(ref syncInputBuffer, NumberOfPlayers);

        for (var i = 0; i < NumberOfPlayers; i++)
            syncInputBuffer[i] = new(input.Data.Inputs[i], false);

        CurrentFrame++;
        return ResultCode.Ok;
    }

    public ref readonly SynchronizedInput<TInput> GetInput(int index) =>
        ref syncInputBuffer[index];

    public ref readonly SynchronizedInput<TInput> GetInput(in PlayerHandle player) =>
        ref syncInputBuffer[player.Number - 1];

    public void Publish(in GameInputEvent<CombinedInputs<TInput>> evt)
    {
        lastReceivedInputTime = clock.GetTimeStamp();
        var (_, input) = evt;
        inputs[input.Frame.Number % inputs.Length] = input;
        host.SetLocalFrameNumber(input.Frame, options.FramesPerSecond);
        host.SendInputAck();
    }
}
