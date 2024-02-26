using System.Diagnostics;
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

sealed class SpectatorBackend<TInput, TGameState> : IRollbackSession<TInput, TGameState>, IProtocolNetworkEventHandler
    where TInput : struct
    where TGameState : IEquatable<TGameState>, new()
{
    readonly RollbackOptions options;
    readonly IBinarySerializer<InputGroup<TInput>> inputSerializer;
    readonly Logger logger;
    readonly IUdpClient<ProtocolMessage> udp;
    readonly UdpObserverGroup<ProtocolMessage> udpObservers;
    readonly IBackgroundJobManager backgroundJobManager;
    readonly IProtocolInputEventQueue<InputGroup<TInput>> peerInputEventQueue;
    readonly PeerConnectionFactory peerConnectionFactory;
    readonly IClock clock;
    readonly ConnectionsState localConnections = new(0);

    readonly List<PlayerHandle> hostPlayer;
    readonly GameInput<InputGroup<TInput>>[] inputs;
    PeerConnection<InputGroup<TInput>>? host;
    IRollbackHandler<TGameState> callbacks;

    bool isSynchronizing = true;
    Task backgroundJobTask = Task.CompletedTask;
    bool disposed;
    long startTimestamp;

    SynchronizedInput<TInput>[] syncInputBuffer = [];

    public SpectatorBackend(
        RollbackOptions options,
        IBinarySerializer<TInput> inputSerializer,
        IUdpClientFactory udpClientFactory,
        IBackgroundJobManager backgroundJobManager,
        IProtocolInputEventQueue<InputGroup<TInput>> peerInputEventQueue,
        IClock clock,
        Logger logger
    )
    {
        this.options = options;
        this.inputSerializer = new InputGroupSerializer<TInput>(inputSerializer);
        this.backgroundJobManager = backgroundJobManager;
        this.peerInputEventQueue = peerInputEventQueue;
        this.logger = logger;
        this.clock = clock;

        callbacks = new EmptySessionHandler<TGameState>(this.logger);
        udpObservers = new();
        hostPlayer = [];
        inputs = new GameInput<InputGroup<TInput>>[options.SpectatorInputBufferLength];

        var selectedEndianness = Platform.GetEndianness(this.options.NetworkEndianness);
        udp = udpClientFactory.CreateClient(
            this.options.LocalPort,
            selectedEndianness,
            this.options.Protocol.UdpPacketBufferSize,
            udpObservers,
            this.logger
        );

        peerConnectionFactory = new(
            this.clock,
            this.options.Random,
            this.logger,
            this.backgroundJobManager,
            this,
            udp,
            this.options.Protocol,
            this.options.TimeSync
        );

        backgroundJobManager.Register(udp);

        if (this.logger.EnabledLevel is not LogLevel.Off && this.logger.RunningAsync)
            backgroundJobManager.Register(this.logger);
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
        host?.Dispose();
    }

    public Frame CurrentFrame { get; private set; } = Frame.Zero;
    public FrameSpan RollbackFrames { get; } = FrameSpan.Zero;
    public int NumberOfPlayers { get; private set; }
    public int NumberOfSpectators => 0;

    public ResultCode AddLocalInput(PlayerHandle player, TInput localInput) => ResultCode.Ok;
    public IReadOnlyCollection<PlayerHandle> GetPlayers() => hostPlayer;
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
        if (!isSynchronizing)
            logger.Write(LogLevel.Debug, $"[Begin Frame {CurrentFrame}]");

        ConsumeProtocolInputEvents();
    }

    public void AdvanceFrame() => logger.Write(LogLevel.Debug, $"[End Frame {CurrentFrame}]");

    public PlayerConnectionStatus GetPlayerStatus(in PlayerHandle player)
    {
        if (player != hostPlayer[0] || host is null)
            return PlayerConnectionStatus.Unknown;

        return host.Status.ToPlayerStatus();
    }

    public ResultCode AddPlayer(Player player)
    {
        if (NumberOfPlayers > 0 || player is not RemotePlayer remotePlayer)
            return ResultCode.NotSupported;

        hostPlayer.Add(player.Handle);
        host = peerConnectionFactory.Create(
            new(player.Handle, remotePlayer.EndPoint, localConnections, options.FramesPerSecond),
            inputSerializer,
            peerInputEventQueue
        );
        udpObservers.Add(host.GetUdpObserver());
        host.Synchronize();
        isSynchronizing = true;

        return ResultCode.Ok;
    }

    public IReadOnlyList<ResultCode> AddPlayers(IReadOnlyList<Player> players)
    {
        var result = new ResultCode[players.Count];
        for (var index = 0; index < players.Count; index++)
            result[index] = AddPlayer(players[index]);
        return result;
    }

    public bool GetNetworkStatus(in PlayerHandle player, ref RollbackNetworkStatus info)
    {
        if (player != hostPlayer[0] || host is null)
            return false;

        host.GetNetworkStats(ref info);
        return true;
    }

    public void SetFrameDelay(PlayerHandle player, int delayInFrames) { }

    public void Start(CancellationToken stoppingToken = default) =>
        backgroundJobTask = backgroundJobManager.Start(stoppingToken);

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

    void ConsumeProtocolInputEvents()
    {
        if (host is null) return;

        while (peerInputEventQueue.TryConsume(out var gameInputEvent))
        {
            var (player, input) = gameInputEvent;
            if (player != hostPlayer[0]) return;

            host.SetLocalFrameNumber(input.Frame);
            host.SendInputAck();

            inputs[input.Frame.Number % inputs.Length] = input;
        }
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
                callbacks.Start();
                startTimestamp = clock.GetTimeStamp();
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
}
