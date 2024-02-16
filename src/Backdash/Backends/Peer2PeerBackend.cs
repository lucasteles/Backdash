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

namespace Backdash.Backends;

sealed class Peer2PeerBackend<TInput, TGameState> : IRollbackSession<TInput, TGameState>
    where TInput : struct
    where TGameState : notnull
{
    readonly RollbackOptions options;
    readonly Logger logger;
    readonly IBinarySerializer<TInput> inputSerializer;
    readonly IUdpClient<ProtocolMessage> udp;
    readonly UdpObserverGroup<ProtocolMessage> udpObservers;
    readonly Synchronizer<TInput, TGameState> synchronizer;
    readonly ConnectionsState localConnections;
    readonly IBackgroundJobManager backgroundJobManager;
    readonly IProtocolEventQueue peerEventQueue;
    readonly bool[] knownLocalPlayers;

    readonly List<PeerConnection> spectators;
    readonly PeerConnection?[] endpoints;

    bool isSynchronizing = true;
    int nextRecommendedInterval;
    Frame nextSpectatorFrame = Frame.Zero;
    IRollbackHandler<TGameState> callbacks;

    Task backgroundJobTask = Task.CompletedTask;

    public Peer2PeerBackend(
        RollbackOptions options,
        IBinarySerializer<TInput> inputSerializer,
        IUdpClientFactory udpClientFactory,
        IBackgroundJobManager backgroundJobManager,
        IProtocolEventQueue peerEventQueue,
        Logger logger
    )
    {
        ThrowHelpers.ThrowIfArgumentIsZeroOrLess(options.LocalPort);
        ThrowHelpers.ThrowIfArgumentIsZeroOrLess(options.NumberOfPlayers);
        ThrowHelpers.ThrowIfTypeTooBigForStack<GameInput>();
        ThrowHelpers.ThrowIfTypeSizeGreaterThan<GameInputBuffer>(Max.InputSizeInBytes);
        ThrowHelpers.ThrowIfTypeTooBigForStack<TInput>();

        var inputTypeSize = inputSerializer.GetTypeSize();
        ThrowHelpers.ThrowIfArgumentOutOfBounds(inputTypeSize, 1, Max.InputSizeInBytes);
        ThrowHelpers.ThrowIfArgumentOutOfBounds(options.NumberOfSpectators, 0, Max.NumberOfSpectators);
        ThrowHelpers.ThrowIfArgumentOutOfBounds(options.SpectatorOffset, options.NumberOfPlayers);

        if (options.NumberOfPlayers > Max.TotalPlayers)
            throw new BackdashException($"Max allowed players for this session is {Max.TotalPlayers}");

        if (options.NumberOfSpectators > Max.NumberOfSpectators)
            throw new BackdashException($"Max allowed spectators for this session is {Max.NumberOfSpectators}");

        this.options = options;
        this.options.InputSize = inputTypeSize;
        this.inputSerializer = inputSerializer;
        this.backgroundJobManager = backgroundJobManager;
        this.peerEventQueue = peerEventQueue;
        this.logger = logger;

        knownLocalPlayers = new bool[Max.TotalPlayers];
        Array.Fill(knownLocalPlayers, false);

        localConnections = new(Max.RemoteConnections);

        udpObservers = new();
        spectators = new(this.options.NumberOfSpectators);
        endpoints = new PeerConnection[this.options.NumberOfPlayers];
        callbacks = new EmptySessionHandler<TGameState>(this.logger);

        synchronizer = new(
            this.options,
            this.logger,
            this.inputSerializer,
            localConnections
        )
        {
            Callbacks = callbacks,
        };

        udp = udpClientFactory.CreateClient(
            this.options.LocalPort,
            this.options.EnableEndianness,
            this.options.Protocol.UdpPacketBufferSize,
            udpObservers,
            this.logger
        );

        backgroundJobManager.Register(udp);

        this.peerEventQueue.Router = RouteEvent;
    }

    bool RouteEvent(ProtocolEvent evt)
    {
        if (evt.Type is ProtocolEventType.Input)
            return false;

        OnProtocolEvent(evt);
        return true;
    }

    public void Dispose()
    {
        logger.Write(LogLevel.Information, "Shutting down connections");
        foreach (var endpoint in endpoints)
            endpoint?.Dispose();

        foreach (var spectator in spectators)
            spectator.Dispose();

        udp.Dispose();
        backgroundJobManager.Dispose();
    }

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
            case Player.Spectator spectator:
                return AddSpectator(spectator);
            case Player.Remote remote:
                return AddRemotePlayer(remote.EndPoint, player);
            case Player.Local local:
                return AddLocalPlayer(local);
            default:
                throw new ArgumentOutOfRangeException(nameof(player));
        }
    }

    public ResultCode AddLocalInput(PlayerHandle player, TInput localInput)
    {
        GameInput input = new(options.InputSize);
        var localInputResult = CreateLocalInput(player, localInput, ref input);

        if (!localInputResult.IsOk())
            return localInputResult;

        // Send the input to all the remote players.
        var sent = true;
        for (var i = 0; i < endpoints.Length; i++)
        {
            if (endpoints[i] is not { } endpoint)
                continue;

            sent = sent && endpoint.SendInput(in input) is AddInputResult.Ok;
        }

        if (!sent)
            return ResultCode.InputDropped;

        return ResultCode.Ok;
    }

    public bool GetInfo(in PlayerHandle player, ref RollbackSessionInfo info)
    {
        if (!IsHandleInRange(in player)) return false;
        if (isSynchronizing) return false;

        info.CurrentFrame = synchronizer.FrameCount.Number;
        info.RollbackFrames = synchronizer.FramesBehind.Number;
        endpoints[player.Index]?.GetNetworkStats(ref info);
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
        ThrowHelpers.ThrowIfArgumentOutOfBounds(player.Number, 1, options.NumberOfPlayers);
        ThrowHelpers.ThrowIfArgumentIsNegative(delayInFrames);
        synchronizer.SetFrameDelay(player, delayInFrames);
    }

    bool IsHandleInRange(in PlayerHandle handle) =>
        handle.Number > 0 || handle.Number <= options.NumberOfPlayers;

    ResultCode AddLocalPlayer(in PlayerHandle handle)
    {
        if (!IsHandleInRange(in handle))
            return ResultCode.PlayerOutOfRange;

        knownLocalPlayers[handle.Index] = true;
        return ResultCode.Ok;
    }

    ResultCode AddRemotePlayer(IPEndPoint endpoint, in PlayerHandle handle)
    {
        if (!IsHandleInRange(in handle))
            return ResultCode.PlayerOutOfRange;

        var protocol = CreatePeerConnection(endpoint, handle);
        endpoints[handle.Index] = protocol;
        logger.Write(LogLevel.Information, $"Adding {handle} at {endpoint.Address}:{endpoint.Port}");
        protocol.Synchronize();

        /*
         * Start the state machine (xxx: no)
         */
        isSynchronizing = true;
        return ResultCode.Ok;
    }

    ResultCode AddSpectator(Player.Spectator spectator)
    {
        if (spectators.Count >= options.NumberOfSpectators)
            return ResultCode.TooManySpectators;

        /*
         * Currently, we can only add spectators before the game starts.
         */
        if (!isSynchronizing)
            return ResultCode.InvalidRequest;

        var queue = spectators.Count + 1;
        PlayerHandle player = new(PlayerType.Spectator, options.SpectatorOffset + queue, queue);
        spectator.Handle = player;
        var protocol = CreatePeerConnection(spectator.EndPoint, player);
        spectators.Add(protocol);
        logger.Write(LogLevel.Information,
            $"Adding {spectator.Handle} at {spectator.EndPoint.Address}:{spectator.EndPoint.Port}");
        protocol.Synchronize();
        return ResultCode.Ok;
    }

    PeerConnection CreatePeerConnection(IPEndPoint endpoint, PlayerHandle player)
    {
        var connection = PeerConnectionFactory.CreateDefault(
            new(player, endpoint, localConnections),
            options.Random,
            logger,
            backgroundJobManager,
            udp,
            peerEventQueue,
            options.Protocol,
            options.TimeSync
        );

        udpObservers.Add(connection.GetUdpObserver());

        return connection;
    }

    ResultCode CreateLocalInput(PlayerHandle player, TInput localInput, ref GameInput input)
    {
        if (isSynchronizing)
            return ResultCode.NotSynchronized;

        if (player.Type is not PlayerType.Local)
            return ResultCode.InvalidPlayerHandle;

        if (!IsHandleInRange(in player))
            return ResultCode.PlayerOutOfRange;

        if (!knownLocalPlayers[player.Index])
            return ResultCode.PlayerUnknown;

        if (synchronizer.InRollback)
            return ResultCode.InRollback;

        var size = inputSerializer.Serialize(ref localInput, input.Buffer);
        input.Size = size;
        Trace.Assert(input.Size == options.InputSize);

        if (!synchronizer.AddLocalInput(in player, ref input))
            return ResultCode.PredictionThreshold;

        // Update the local connect status state to indicate that we've got a
        // confirmed local frame for this player.  this must come first so it
        // gets incorporated into the next packet we send.
        if (input.Frame.IsNull)
            return ResultCode.Ok;

        logger.Write(LogLevel.Trace,
            $"setting local connect status for local queue {player.Index} to {input.Frame}");

        localConnections[player].LastFrame = input.Frame;
        return ResultCode.Ok;
    }

    public bool IsConnected(in PlayerHandle player) => localConnections.IsConnected(in player);

    public void BeginFrame()
    {
        logger.Write(LogLevel.Trace, $"Start of frame ({synchronizer.FrameCount})...");
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
        for (var i = 0; i < endpoints.Length; i++)
        {
            if (endpoints[i] is { IsRunning: false } ep && localConnections.IsConnected(ep.Player))
                return;
        }

        for (var i = 0; i < spectators.Count; i++)
        {
            if (!spectators[i].IsRunning)
                return;
        }

        callbacks.OnEvent(new()
        {
            Type = RollbackEventType.Running,
        });

        isSynchronizing = false;
    }

    public void AdvanceFrame()
    {
        logger.Write(LogLevel.Trace, $"End of frame ({synchronizer.FrameCount})...");
        synchronizer.IncrementFrame();
        DoSync();
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

        for (var i = 0; i < endpoints.Length; i++)
            endpoints[i]?.Update();

        ConsumeProtocolEvents();

        if (isSynchronizing)
            return;

        synchronizer.CheckSimulation();
        // notify all of our endpoints of their local frame number for their
        // next connection quality report
        var currentFrame = synchronizer.FrameCount;
        for (var i = 0; i < endpoints.Length; i++)
            endpoints[i]?.SetLocalFrameNumber(currentFrame);

        var totalMinConfirmed = options.NumberOfPlayers <= 2
            ? SyncPlayers()
            : SyncNPlayers();

        logger.Write(LogLevel.Trace, $"last confirmed frame in p2p backend is {totalMinConfirmed}");
        if (totalMinConfirmed >= Frame.Zero)
        {
            if (options.NumberOfSpectators > 0)
                while (nextSpectatorFrame <= totalMinConfirmed)
                {
                    logger.Write(LogLevel.Trace, $"pushing frame {nextSpectatorFrame} to spectators.\n");

                    for (var playerNumber = 0; playerNumber < options.NumberOfPlayers; playerNumber++)
                    {
                        if (!synchronizer.GetConfirmedInput(in nextSpectatorFrame, playerNumber, out var confirmed))
                            continue;

                        // LATER: should send all grouped inputs to spectators like GGPO?
                        for (int s = 0; s < spectators.Count; s++)
                            spectators[s].SendInput(confirmed);
                    }

                    nextSpectatorFrame++;
                }

            logger.Write(LogLevel.Debug, $"setting confirmed frame in sync to {totalMinConfirmed}");
            synchronizer.SetLastConfirmedFrame(totalMinConfirmed);
        }

        // send time sync notifications if now is the proper time
        if (currentFrame.Number > nextRecommendedInterval)
        {
            int interval = 0;
            for (var i = 0; i < endpoints.Length; i++)
                if (endpoints[i] is { } endpoint)
                    interval = Math.Max(interval, endpoint.RecommendFrameDelay);

            if (interval <= 0) return;
            callbacks.OnEvent(new()
            {
                Type = RollbackEventType.TimeSync,
                TimeSync = new(interval),
            });
            nextRecommendedInterval = currentFrame.Number + options.RecommendationInterval;
        }
    }

    void OnProtocolEvent(in ProtocolEvent evt)
    {
        ref readonly var player = ref evt.Player;

        switch (evt.Type)
        {
            case ProtocolEventType.Connected:
                callbacks.OnEvent(new()
                {
                    Type = RollbackEventType.ConnectedToPeer,
                    Connected = new(player),
                });
                break;
            case ProtocolEventType.Synchronizing:
                callbacks.OnEvent(new()
                {
                    Type = RollbackEventType.SynchronizingWithPeer,
                    Synchronizing = new(player, evt.Synchronizing.Count, evt.Synchronizing.Total),
                });
                break;
            case ProtocolEventType.Synchronized:
                callbacks.OnEvent(new()
                {
                    Type = RollbackEventType.SynchronizedWithPeer,
                    Synchronized = new(player),
                });
                CheckInitialSync();
                break;
            case ProtocolEventType.NetworkInterrupted:
                callbacks.OnEvent(new()
                {
                    Type = RollbackEventType.ConnectionInterrupted,
                    ConnectionInterrupted = new(player, evt.NetworkInterrupted.DisconnectTimeout),
                });
                break;
            case ProtocolEventType.NetworkResumed:
                callbacks.OnEvent(new()
                {
                    Type = RollbackEventType.ConnectionResumed,
                    ConnectionResumed = new(player),
                });
                break;
            case ProtocolEventType.Disconnected:
                if (player.Type is PlayerType.Spectator)
                    spectators[player.Index].Disconnect();

                if (player.Type is PlayerType.Remote)
                    DisconnectPlayer(player);

                callbacks.OnEvent(new()
                {
                    Type = RollbackEventType.DisconnectedFromPeer,
                    Disconnected = new(player),
                });
                break;
            case ProtocolEventType.Input when player.Type is PlayerType.Remote:

                if (localConnections[player].Disconnected)
                    break;

                var currentRemoteFrame = localConnections[player].LastFrame;
                var newRemoteFrame = evt.Input.Frame;
                Trace.Assert(currentRemoteFrame.IsNull || newRemoteFrame == currentRemoteFrame.Next());

                var eventInput = evt.Input;
                synchronizer.AddRemoteInput(in player, eventInput);
                // Notify the other endpoints which frame we received from a peer
                logger.Write(LogLevel.Trace,
                    $"setting remote connect status frame {player} to {eventInput.Frame}");
                localConnections[player].LastFrame = eventInput.Frame;
                break;
            case ProtocolEventType.Input:
                logger.Write(LogLevel.Warning, $"non-remote input received from {player}");
                break;
            default:
                logger.Write(LogLevel.Warning, $"Unknown protocol event {evt} from {player}");
                break;
        }
    }

    Frame SyncPlayers()
    {
        // discard confirmed frames as appropriate
        Frame totalMinConfirmed = Frame.MaxValue;
        for (var i = 0; i < endpoints.Length; i++)
        {
            if (endpoints[i] is not { } endpoint)
                continue;

            var queueConnected = true;
            if (endpoint.IsRunning) queueConnected = endpoint.GetPeerConnectStatus(i, out _);

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

    Frame SyncNPlayers()
    {
        // discard confirmed frames as appropriate
        var totalMinConfirmed = Frame.MaxValue;
        for (var queue = 0; queue < options.NumberOfPlayers; queue++)
        {
            var queueConnected = true;
            var queueMinConfirmed = Frame.MaxValue;
            logger.Write(LogLevel.Trace, $"considering queue {queue}");
            for (var i = 0; i < endpoints.Length; i++)
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
        var frameCount = synchronizer.FrameCount;

        endpoints[player.Index]?.Disconnect();

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

        callbacks.OnEvent(new()
        {
            Type = RollbackEventType.DisconnectedFromPeer,
            Disconnected = new RollbackEvent.PlayerEventInfo(player),
        });

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

        if (endpoints[player.Index] is null)
        {
            var currentFrame = synchronizer.FrameCount;
            // xxx: we should be tracking who the local player is, but for now assume
            // that if the endpoint is not initalized, this must be the local player.
            logger.Write(LogLevel.Information,
                $"Disconnecting {player} at frame {localConnections[player].LastFrame} by user request.");
            for (int i = 0; i < endpoints.Length; i++)
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
