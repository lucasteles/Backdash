using System.Diagnostics;
using System.Net;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol;
using Backdash.Network.Protocol.Messaging;
using Backdash.Serialization;
using Backdash.Sync;
using Backdash.Sync.State;

namespace Backdash.Backends;

sealed class Peer2PeerBackend<TInput, TGameState> : IRollbackSession<TInput, TGameState>
    where TInput : struct
    where TGameState : IEquatable<TGameState>, new()
{
    readonly RollbackOptions options;
    readonly Logger logger;
    readonly IStateStore<TGameState> stateStore;
    readonly IUdpClient<ProtocolMessage> udp;
    readonly UdpObserverGroup<ProtocolMessage> udpObservers;
    readonly Synchronizer<TInput, TGameState> synchronizer;
    readonly ConnectionsState localConnections;
    readonly IBackgroundJobManager backgroundJobManager;
    readonly IProtocolEventQueue<TInput> peerEventQueue;
    readonly PeerConnectionFactory<TInput> peerConnectionFactory;
    readonly IClock clock;
    readonly List<PeerConnection<TInput>> spectators;
    readonly List<PeerConnection<TInput>?> endpoints;

    readonly HashSet<PlayerHandle> addedPlayers = new();
    readonly HashSet<PlayerHandle> addedSpectators = new();

    bool isSynchronizing = true;
    int nextRecommendedInterval;
    long startTimestamp;
    Frame nextSpectatorFrame = Frame.Zero;
    IRollbackHandler<TGameState> callbacks;

    Task backgroundJobTask = Task.CompletedTask;

    public Peer2PeerBackend(
        RollbackOptions options,
        IBinarySerializer<TInput> inputSerializer,
        IStateStore<TGameState> stateStore,
        IChecksumProvider<TGameState> checksumProvider,
        IUdpClientFactory udpClientFactory,
        IBackgroundJobManager backgroundJobManager,
        IProtocolEventQueue<TInput> peerEventQueue,
        IClock clock,
        Logger logger
    )
    {
        ThrowHelpers.ThrowIfArgumentIsZeroOrLess(options.LocalPort);
        ThrowHelpers.ThrowIfArgumentIsZeroOrLess(options.FramesPerSecond);
        ThrowHelpers.ThrowIfArgumentOutOfBounds(options.SpectatorOffset, min: Max.Players);
        ThrowHelpers.ThrowIfTypeTooBigForStack<GameInput<TInput>>();

        this.options = options;
        this.stateStore = stateStore;
        this.backgroundJobManager = backgroundJobManager;
        this.peerEventQueue = peerEventQueue;
        this.logger = logger;
        this.clock = clock;

        localConnections = new(Max.RemoteConnections);

        udpObservers = new();
        spectators = new();
        endpoints = new();
        callbacks = new EmptySessionHandler<TGameState>(this.logger);

        synchronizer = new(
            this.options,
            this.logger,
            addedPlayers,
            stateStore,
            checksumProvider,
            localConnections
        )
        {
            Callbacks = callbacks,
        };

        var selectedEndianness = Platform.GetEndianness(this.options.NetworkEndianness);

        udp = udpClientFactory.CreateClient(
            this.options.LocalPort,
            selectedEndianness,
            this.options.Protocol.UdpPacketBufferSize,
            udpObservers,
            this.logger
        );

        peerConnectionFactory = new(
            inputSerializer,
            this.clock,
            this.options.Random,
            this.logger,
            this.backgroundJobManager,
            udp,
            this.peerEventQueue,
            this.options.Protocol,
            this.options.TimeSync
        );

        this.peerEventQueue.ProxyFilter = RouteEvent;

        backgroundJobManager.Register(udp);

        if (this.logger.EnabledLevel is not LogLevel.Off && this.logger.RunningAsync)
            backgroundJobManager.Register(this.logger);
    }

    bool RouteEvent(ProtocolEventInfo<TInput> evt)
    {
        if (evt.Type is ProtocolEvent.Input)
            return false;

        OnProtocolEvent(evt);
        return true;
    }

    public void Dispose()
    {
        logger.Write(LogLevel.Information, "Shutting down connections");
        backgroundJobManager.Dispose();

        foreach (var endpoint in endpoints)
            endpoint?.Dispose();

        foreach (var spectator in spectators)
            spectator.Dispose();

        logger.Dispose();
        udp.Dispose();
        stateStore.Dispose();
    }

    public Frame CurrentFrame => synchronizer.CurrentFrame;

    public int FramesPerSecond
    {
        get
        {
            var elapsed = clock.GetElapsedTime(startTimestamp).TotalSeconds;
            return elapsed > 0 ? (int)(synchronizer.CurrentFrame.Number / elapsed) : 0;
        }
    }

    public FrameSpan RollbackFrames => FrameSpan.Max(synchronizer.FramesBehind, FrameSpan.Zero);
    public int NumberOfPlayers => addedPlayers.Count;
    public int NumberOfSpectators => addedSpectators.Count;

    public IReadOnlyCollection<PlayerHandle> GetPlayers() => addedPlayers;
    public IReadOnlyCollection<PlayerHandle> GetSpectators() => addedSpectators;

    public void Start(CancellationToken stoppingToken = default) =>
        backgroundJobTask = backgroundJobManager.Start(stoppingToken);

    public async Task WaitToStop(CancellationToken stoppingToken = default)
    {
        backgroundJobManager.Stop(TimeSpan.Zero);
        await backgroundJobTask.WaitAsync(stoppingToken).ConfigureAwait(false);
    }

    public ResultCode AddPlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        switch (player)
        {
            case Spectator spectator:
                return AddSpectator(spectator);
            case RemotePlayer remote:
                return AddRemotePlayer(remote);
            case LocalPlayer local:
                return AddLocalPlayer(local);
            default:
                throw new ArgumentOutOfRangeException(nameof(player));
        }
    }

    public ResultCode AddLocalInput(PlayerHandle player, TInput localInput)
    {
        var localInputResult = CreateLocalInput(player, localInput, out var input);

        if (!localInputResult.IsOk())
            return localInputResult;

        // Send the input to all the remote players.
        var sent = true;
        for (var i = 0; i < endpoints.Count; i++)
        {
            if (endpoints[i] is not { } endpoint)
                continue;

            var result = endpoint.SendInput(in input);
            if (result is AddInputResult.Ok) continue;

            sent = false;
            logger.Write(LogLevel.Warning, $"Unable to send input to queue {i}, {result}");
        }

        if (!sent)
            return ResultCode.InputDropped;

        return ResultCode.Ok;
    }

    bool IsPlayerKnown(in PlayerHandle player) =>
        player.InternalQueue >= 0
        && player.InternalQueue <
        player.Type switch
        {
            PlayerType.Remote => endpoints.Count,
            PlayerType.Spectator => spectators.Count,
            _ => int.MaxValue,
        };

    public bool GetNetworkStatus(in PlayerHandle player, ref RollbackNetworkStatus info)
    {
        if (!IsPlayerKnown(in player)) return false;
        if (isSynchronizing) return false;

        endpoints[player.InternalQueue]?.GetNetworkStats(ref info);
        return true;
    }

    public void SetHandler(IRollbackHandler<TGameState> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        callbacks = handler;
        synchronizer.Callbacks = handler;
    }

    public void SetFrameDelay(PlayerHandle player, int delayInFrames)
    {
        ThrowHelpers.ThrowIfArgumentOutOfBounds(player.InternalQueue, 0, addedPlayers.Count);
        ThrowHelpers.ThrowIfArgumentIsNegative(delayInFrames);
        synchronizer.SetFrameDelay(player, delayInFrames);
    }

    ResultCode AddLocalPlayer(in LocalPlayer player)
    {
        if (addedPlayers.Count >= Max.Players)
            return ResultCode.TooManyPlayers;

        PlayerHandle handle = new(player.Handle.Type, player.Handle.Number, addedPlayers.Count);

        if (!addedPlayers.Add(handle))
            return ResultCode.Duplicated;

        player.Handle = handle;
        endpoints.Add(null);
        synchronizer.AddQueue();
        return ResultCode.Ok;
    }

    ResultCode AddRemotePlayer(RemotePlayer player)
    {
        if (addedPlayers.Count >= Max.Players)
            return ResultCode.TooManyPlayers;

        PlayerHandle handle = new(player.Handle.Type, player.Handle.Number, addedPlayers.Count);

        if (!addedPlayers.Add(handle))
            return ResultCode.Duplicated;

        player.Handle = handle;
        var endpoint = player.EndPoint;
        var protocol = CreatePeerConnection(endpoint, player.Handle);
        endpoints.Add(protocol);
        synchronizer.AddQueue();
        logger.Write(LogLevel.Information, $"Adding {player.Handle} at {endpoint.Address}:{endpoint.Port}");
        protocol.Synchronize();

        /*
         * Start the state machine (xxx: no)
         */
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

    ResultCode AddSpectator(Spectator spectator)
    {
        if (spectators.Count >= Max.NumberOfSpectators)
            return ResultCode.TooManySpectators;

        /*
         * Currently, we can only add spectators before the game starts.
         */
        if (!isSynchronizing)
            return ResultCode.InvalidRequest;

        var queue = spectators.Count;
        PlayerHandle spectatorHandle = new(PlayerType.Spectator, options.SpectatorOffset + queue, queue);
        if (!addedSpectators.Add(spectatorHandle))
            return ResultCode.Duplicated;

        spectator.Handle = spectatorHandle;
        var protocol = CreatePeerConnection(spectator.EndPoint, spectatorHandle);
        spectators.Add(protocol);
        logger.Write(LogLevel.Information,
            $"Adding {spectator.Handle} at {spectator.EndPoint.Address}:{spectator.EndPoint.Port}");
        protocol.Synchronize();
        return ResultCode.Ok;
    }

    PeerConnection<TInput> CreatePeerConnection(IPEndPoint endpoint, PlayerHandle player)
    {
        var connection = peerConnectionFactory.Create(new(player, endpoint, localConnections, options.FramesPerSecond));
        udpObservers.Add(connection.GetUdpObserver());
        return connection;
    }

    ResultCode CreateLocalInput(PlayerHandle player, TInput localInput, out GameInput<TInput> input)
    {
        input = new()
        {
            Data = localInput,
        };

        if (isSynchronizing)
            return ResultCode.NotSynchronized;

        if (player.Type is not PlayerType.Local)
            return ResultCode.InvalidPlayerHandle;

        if (!IsPlayerKnown(in player))
            return ResultCode.PlayerOutOfRange;

        if (synchronizer.InRollback)
            return ResultCode.InRollback;


        if (!synchronizer.AddLocalInput(in player, ref input))
            return ResultCode.PredictionThreshold;

        // Update the local connect status state to indicate that we've got a
        // confirmed local frame for this player.  this must come first so it
        // gets incorporated into the next packet we send.
        if (input.Frame.IsNull)
            return ResultCode.Ok;

        logger.Write(LogLevel.Trace,
            $"setting local connect status for local queue {player.InternalQueue} to {input.Frame}");

        localConnections[player].LastFrame = input.Frame;
        return ResultCode.Ok;
    }

    public PlayerStatus GetPlayerStatus(in PlayerHandle player)
    {
        if (!IsPlayerKnown(in player)) return PlayerStatus.Unknown;
        if (player.IsLocal()) return PlayerStatus.Local;
        if (player.IsSpectator()) return spectators[player.InternalQueue].Status.ToPlayerStatus();

        var endpoint = endpoints[player.InternalQueue];

        if (endpoint?.IsRunning == true)
            return localConnections.IsConnected(in player) ? PlayerStatus.Connected : PlayerStatus.Disconnected;

        return endpoint?.Status.ToPlayerStatus() ?? PlayerStatus.Unknown;
    }

    public void BeginFrame()
    {
        if (!isSynchronizing)
            logger.Write(LogLevel.Trace, $"Start of frame ({synchronizer.CurrentFrame})...");

        DoSync();
    }

    public ResultCode SynchronizeInputs(Span<TInput> inputs)
    {
        if (isSynchronizing)
            return ResultCode.NotSynchronized;

        synchronizer.SynchronizeInputs(inputs, out var anyDisconnection);

        return anyDisconnection ? ResultCode.PlayerDisconnected : ResultCode.Ok;
    }

    void CheckInitialSync()
    {
        if (!isSynchronizing) return;

        // Check to see if everyone is now synchronized.  If so,
        // go ahead and tell the client that we're ok to accept input.
        for (var i = 0; i < endpoints.Count; i++)
            if (endpoints[i] is { IsRunning: false } ep && localConnections.IsConnected(ep.Player))
                return;

        for (var i = 0; i < spectators.Count; i++)
            if (!spectators[i].IsRunning)
                return;

        isSynchronizing = false;
        startTimestamp = clock.GetTimeStamp();
        callbacks.Start();
    }

    public void AdvanceFrame()
    {
        logger.Write(LogLevel.Trace, $"End of frame ({synchronizer.CurrentFrame})...");
        synchronizer.IncrementFrame();
    }

    void ConsumeProtocolEvents()
    {
        while (peerEventQueue.TryConsume(out var nextEvent))
            OnProtocolEvent(in nextEvent);
    }

    void DoSync()
    {
        if (synchronizer.InRollback)
            return;

        ConsumeProtocolEvents();

        for (var i = 0; i < endpoints.Count; i++)
            endpoints[i]?.Update();

        if (isSynchronizing)
            return;

        synchronizer.CheckSimulation();
        // notify all of our endpoints of their local frame number for their
        // next connection quality report
        var currentFrame = synchronizer.CurrentFrame;
        for (var i = 0; i < endpoints.Count; i++)
            endpoints[i]?.SetLocalFrameNumber(currentFrame, options.FramesPerSecond);

        var minConfirmedFrame = NumberOfPlayers <= 2 ? MinimumFrame2Players() : MinimumFrameNPlayers();
        Trace.Assert(minConfirmedFrame != Frame.MaxValue);

        logger.Write(LogLevel.Trace, $"last confirmed frame in p2p backend is {minConfirmedFrame}");
        if (minConfirmedFrame >= Frame.Zero)
        {
            if (NumberOfSpectators > 0)
                while (nextSpectatorFrame <= minConfirmedFrame)
                {
                    logger.Write(LogLevel.Trace, $"pushing frame {nextSpectatorFrame} to spectators.\n");

                    for (var playerNumber = 0; playerNumber < NumberOfPlayers; playerNumber++)
                    {
                        if (!synchronizer.GetConfirmedInput(in nextSpectatorFrame, playerNumber, out var confirmed))
                            continue;

                        // LATER: should send all grouped inputs to spectators like GGPO?
                        for (int s = 0; s < spectators.Count; s++)
                            spectators[s].SendInput(confirmed);
                    }

                    nextSpectatorFrame++;
                }

            logger.Write(LogLevel.Debug, $"setting confirmed frame in sync to {minConfirmedFrame}");
            synchronizer.SetLastConfirmedFrame(minConfirmedFrame);
        }

        // send time sync notifications if now is the proper time
        if (currentFrame.Number > nextRecommendedInterval)
        {
            var interval = 0;
            for (var i = 0; i < endpoints.Count; i++)
                if (endpoints[i] is { } endpoint)
                    interval = Math.Max(interval, endpoint.GetRecommendFrameDelay(options.RequireIdleInput));

            if (interval <= 0) return;
            callbacks.TimeSync(new(interval, options.FramesPerSecond));
            nextRecommendedInterval = currentFrame.Number + options.RecommendationInterval;
        }
    }

    void OnProtocolEvent(in ProtocolEventInfo<TInput> evt)
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
                CheckInitialSync();
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
                if (player.Type is PlayerType.Spectator)
                    spectators[player.InternalQueue].Disconnect();

                if (player.Type is PlayerType.Remote)
                    DisconnectPlayer(player);

                callbacks.OnPeerEvent(player, new(PeerEvent.Disconnected));
                break;
            case ProtocolEvent.Input when player.Type is PlayerType.Remote:

                if (localConnections[player].Disconnected)
                    break;

                var currentRemoteFrame = localConnections[player].LastFrame;
                var eventInput = evt.Input;
                var newRemoteFrame = eventInput.Frame;
                Trace.Assert(currentRemoteFrame.IsNull || newRemoteFrame == currentRemoteFrame.Next());

                synchronizer.AddRemoteInput(in player, eventInput);
                // Notify the other endpoints which frame we received from a peer
                logger.Write(LogLevel.Trace,
                    $"setting remote connect status frame {player} to {eventInput.Frame}");
                localConnections[player].LastFrame = eventInput.Frame;
                break;
            case ProtocolEvent.Input:
                logger.Write(LogLevel.Warning, $"non-remote input received from {player}");
                break;
            default:
                logger.Write(LogLevel.Warning, $"Unknown protocol event {evt} from {player}");
                break;
        }
    }

    Frame MinimumFrame2Players()
    {
        // discard confirmed frames as appropriate
        Frame totalMinConfirmed = Frame.MaxValue;
        for (var i = 0; i < endpoints.Count; i++)
        {
            var queueConnected = true;
            if (endpoints[i] is { IsRunning: true } endpoint)
                queueConnected = endpoint.GetPeerConnectStatus(i, out _);

            ref var localConn = ref localConnections[i];
            if (!localConn.Disconnected)
                totalMinConfirmed = Frame.Min(in localConnections[i].LastFrame, in totalMinConfirmed);

            logger.Write(LogLevel.Trace,
                $"[Endpoint {i}] connected = {!localConn.Disconnected}; last received = {localConn.LastFrame}; total min confirmed = {totalMinConfirmed}");

            if (!queueConnected && !localConn.Disconnected)
            {
                logger.Write(LogLevel.Information, $"disconnecting {i} by remote request");
                PlayerHandle handle = new(PlayerType.Remote, i);
                DisconnectPlayerQueue(in handle, in totalMinConfirmed);
            }

            logger.Write(LogLevel.Trace, $"[Endpoint {i}] total min confirmed = {totalMinConfirmed}");
        }

        return totalMinConfirmed;
    }

    Frame MinimumFrameNPlayers()
    {
        // discard confirmed frames as appropriate
        var totalMinConfirmed = Frame.MaxValue;
        for (var queue = 0; queue < NumberOfPlayers; queue++)
        {
            var queueConnected = true;
            var queueMinConfirmed = Frame.MaxValue;
            logger.Write(LogLevel.Trace, $"considering queue {queue}");
            for (var i = 0; i < endpoints.Count; i++)
            {
                // we're going to do a lot of logic here in consideration of endpoint i.
                // keep accumulating the minimum confirmed point for all n*n packets and
                // throw away the rest.
                if (endpoints[i] is { IsRunning: true } endpoint)
                {
                    var connected = endpoint.GetPeerConnectStatus(queue, out var lastReceived);
                    queueConnected = queueConnected && connected;
                    queueMinConfirmed = Frame.Min(in lastReceived, in queueMinConfirmed);
                    logger.Write(LogLevel.Trace,
                        $"[Endpoint {i}] connected = {connected}; last received = {lastReceived}; queue min confirmed = {queueMinConfirmed}");
                }
                else
                    logger.Write(LogLevel.Trace, $"[Endpoint {i}] ignoring... not running.");
            }

            ref var localStatus = ref localConnections[queue];

            // merge in our local status only if we're still connected!
            if (!localStatus.Disconnected)
                queueMinConfirmed = Frame.Min(in localStatus.LastFrame, in queueMinConfirmed);

            logger.Write(LogLevel.Trace,
                $"[Endpoint {queue}]: connected = {!localStatus.Disconnected}; last received = {localStatus.LastFrame}, queue min confirmed = {queueMinConfirmed}");

            if (queueConnected)
                totalMinConfirmed = Frame.Min(in queueMinConfirmed, in totalMinConfirmed);
            else
            {
                // check to see if this disconnect notification is further back than we've been before.  If
                // so, we need to re-adjust.  This can happen when we detect our own disconnect at frame n
                // and later receive a disconnect notification for frame n-1.
                if (!localStatus.Disconnected || localStatus.LastFrame > queueMinConfirmed)
                {
                    logger.Write(LogLevel.Information, $"disconnecting queue {queue} by remote request");
                    PlayerHandle handle = new(PlayerType.Remote, queue);
                    DisconnectPlayerQueue(in handle, in queueMinConfirmed);
                }
            }

            logger.Write(LogLevel.Trace, $"[Endpoint {queue}] total min confirmed = {totalMinConfirmed}");
        }

        return totalMinConfirmed;
    }

    void DisconnectPlayerQueue(in PlayerHandle player, in Frame syncTo)
    {
        var frameCount = synchronizer.CurrentFrame;

        endpoints[player.InternalQueue]?.Disconnect();

        ref var connStatus = ref localConnections[player];

        logger.Write(LogLevel.Debug,
            $"Changing player {player} local connect status for last frame from {connStatus.LastFrame} to {syncTo} on disconnect request (current: {frameCount})");

        connStatus.Disconnected = true;
        connStatus.LastFrame = syncTo;

        if (syncTo < frameCount)
        {
            logger.Write(LogLevel.Information,
                $"adjusting simulation to account for the fact that {player} disconnected @ {syncTo}");
            synchronizer.AdjustSimulation(in syncTo);
            logger.Write(LogLevel.Information, "finished adjusting simulation.");
        }

        callbacks.OnPeerEvent(player, new(PeerEvent.Disconnected));

        CheckInitialSync();
    }

    /*
     * Called only as the result of a local decision to disconnect.  The remote
     * decisions to disconnect are a result of us parsing the peer_connect_settings
     * blob in every endpoint periodically.
     */
    void DisconnectPlayer(in PlayerHandle player)
    {
        if (localConnections[player].Disconnected)
            return;

        if (player.Type is not PlayerType.Remote)
            return;

        if (endpoints[player.InternalQueue] is null)
        {
            var currentFrame = synchronizer.CurrentFrame;
            // xxx: we should be tracking who the local player is, but for now assume
            // that if the endpoint is not initalized, this must be the local player.
            logger.Write(LogLevel.Information,
                $"Disconnecting {player} at frame {localConnections[player].LastFrame} by user request.");
            for (int i = 0; i < endpoints.Count; i++)
                if (endpoints[i] is not null)
                    DisconnectPlayerQueue(new(PlayerType.Remote, i), currentFrame);
        }
        else
        {
            logger.Write(LogLevel.Information,
                $"Disconnecting {player} at frame {localConnections[player].LastFrame} by user request.");
            DisconnectPlayerQueue(player, localConnections[player].LastFrame);
        }
    }
}
