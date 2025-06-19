using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Core;
using Backdash.Network;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol;
using Backdash.Network.Protocol.Comm;
using Backdash.Options;
using Backdash.Serialization;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.Input.Confirmed;
using Backdash.Synchronizing.Random;
using Backdash.Synchronizing.State;

namespace Backdash.Backends;

sealed class RemoteSession<TInput> : INetcodeSession<TInput>
    where TInput : unmanaged
{
    readonly NetcodeOptions options;
    readonly Logger logger;
    readonly PeerClient<ProtocolMessage> udp;
    readonly Synchronizer<TInput> synchronizer;
    readonly NetcodeJobManager jobManager;
    readonly PeerConnectionFactory peerConnectionFactory;
    readonly IDeterministicRandom<TInput> random;
    readonly ProtocolCombinedInputsEventPublisher<TInput> peerCombinedInputsEventPublisher;
    readonly PeerObserverGroup<ProtocolMessage> peerObservers;
    readonly ProtocolInputEventQueue<TInput> peerInputEventQueue;

    readonly ConnectionsState localConnections;
    readonly List<PeerConnection<TInput>?> endpoints;
    readonly List<PeerConnection<ConfirmedInputs<TInput>>> spectators;

    readonly HashSet<NetcodePlayer> addedPlayers = [];
    readonly HashSet<NetcodePlayer> addedSpectators = [];
    readonly Dictionary<Guid, NetcodePlayer> allPlayers = [];

    readonly IBinarySerializer<TInput> inputSerializer;
    readonly IBinarySerializer<ConfirmedInputs<TInput>> inputGroupSerializer;
    readonly EqualityComparer<TInput> inputComparer;
    readonly EqualityComparer<ConfirmedInputs<TInput>> inputGroupComparer;
    readonly IInputListener<TInput>? inputListener;
    readonly ProtocolNetworkEventQueue networkEventQueue;
    readonly PluginManager plugins;

    bool isSynchronizing = true;
    int nextRecommendedInterval;
    Frame nextSpectatorFrame = Frame.Zero;
    Frame nextListenerFrame = Frame.Zero;
    INetcodeSessionHandler callbacks;
    SynchronizedInput<TInput>[] syncInputBuffer = [];
    TInput[] inputBuffer = [];
    Task backgroundJobTask = Task.CompletedTask;
    readonly ushort syncNumber;
    bool started;
    bool disposed;
    bool closed;

    public int FixedFrameRate { get; }

    public RemoteSession(
        NetcodeOptions options,
        SessionServices<TInput> services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.LocalPort);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.FrameRate);

        this.options = options;
        FixedFrameRate = this.options.FrameRate;
        inputSerializer = services.InputSerializer;
        jobManager = services.JobManager;
        logger = services.Logger;
        inputListener = services.InputListener;
        random = services.DeterministicRandom;
        inputComparer = services.InputComparer;
        plugins = services.PluginManager;

        peerInputEventQueue = new();
        networkEventQueue = new();
        inputGroupComparer = ConfirmedInputComparer<TInput>.Create(services.InputComparer);
        syncNumber = services.Random.SyncNumber();
        peerCombinedInputsEventPublisher = new(peerInputEventQueue);
        inputGroupSerializer = new ConfirmedInputsSerializer<TInput>(inputSerializer);
        localConnections = new(Max.NumberOfPlayers);
        endpoints = new(Max.NumberOfPlayers);
        spectators = [];
        peerObservers = new();
        callbacks = services.SessionHandler;

        synchronizer = new(
            this.options,
            logger,
            addedPlayers,
            services.StateStore,
            services.ChecksumProvider,
            localConnections,
            inputComparer
        )
        {
            Callbacks = callbacks,
        };

        udp = services.ProtocolClientFactory.CreateClient(options.LocalPort, peerObservers);

        peerConnectionFactory = new(
            networkEventQueue,
            services.Random,
            logger,
            udp,
            this.options.Protocol,
            this.options.TimeSync,
            services.StateStore
        );

        ConfigureJobs(services);
    }

    void ConfigureJobs(SessionServices<TInput> services)
    {
        jobManager.Register(udp);

        // ReSharper disable once SuspiciousTypeConversion.Global
        if (udp.Socket is INetcodeJob socketJob)
            jobManager.Register(socketJob);

        foreach (var job in services.Jobs)
            jobManager.Register(job);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Close();
        udp.Dispose();
        DisposeInternal();
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        Close();
        await udp.DisposeAsync();
        DisposeInternal();
        await WaitToStop();
    }

    void DisposeInternal()
    {
        logger.Dispose();
        jobManager.Dispose();
        networkEventQueue.Dispose();
        inputListener?.Dispose();
    }

    public void Close()
    {
        if (closed) return;
        closed = true;

        logger.Write(LogLevel.Information, "Shutting down connections");

        plugins.OnClose(this);

        foreach (var endpoint in endpoints)
        {
            if (endpoint is null) continue;
            plugins.OnEndpointClosed(this, endpoint.Address, endpoint.Player);
            endpoint.Dispose();
        }

        foreach (var spectator in spectators)
        {
            plugins.OnEndpointClosed(this, spectator.Address, spectator.Player);
            spectator.Dispose();
        }

        callbacks.OnSessionClose();
        inputListener?.OnSessionClose();
    }

    public INetcodeRandom Random => random;
    public Frame CurrentFrame => synchronizer.CurrentFrame;
    public FrameSpan FramesBehind => synchronizer.FramesBehind;
    public FrameSpan RollbackFrames => synchronizer.RollbackFrames;
    public bool IsInRollback => synchronizer.InRollback;
    public SavedFrame GetCurrentSavedFrame() => synchronizer.GetLastSavedFrame();
    public int NumberOfPlayers => addedPlayers.Count;
    public int NumberOfSpectators => addedSpectators.Count;

    public int LocalPort => udp.BindPort;

    public SessionMode Mode => SessionMode.Remote;

    public IReadOnlySet<NetcodePlayer> GetPlayers() => addedPlayers;
    public IReadOnlySet<NetcodePlayer> GetSpectators() => addedSpectators;
    public NetcodePlayer? FindPlayer(Guid id) => allPlayers.GetValueOrDefault(id);

    public bool TryGetPlayer(PlayerType playerType, [NotNullWhen(true)] out NetcodePlayer? player)
    {
        foreach (var p in addedPlayers)
        {
            if (p.Type != playerType) continue;
            player = p;
            return true;
        }

        player = null;
        return false;
    }

    public void Start(CancellationToken stoppingToken = default)
    {
        if (started) return;
        started = true;

        plugins.OnStart(this);
        inputListener?.OnSessionStart(in inputSerializer);
        backgroundJobTask = jobManager.Start(options.UseBackgroundThread, stoppingToken);
    }

    public Task WaitToStop(CancellationToken stoppingToken = default)
    {
        if (!jobManager.IsRunning)
            return Task.CompletedTask;

        jobManager.Stop(TimeSpan.Zero);
        return backgroundJobTask.WaitAsync(stoppingToken);
    }

    public void BeginFrame()
    {
        if (!isSynchronizing)
            logger.Write(LogLevel.Trace, $"[Begin Frame {synchronizer.CurrentFrame}]");

        DoSync();
    }

    public void WriteLog(LogLevel level, string message) => logger.Write(level, message);
    public void WriteLog(string message, Exception? error = null) => logger.Write(message, error);

    public ResultCode AddPlayer(NetcodePlayer player)
    {
        var result = player.Type switch
        {
            PlayerType.Spectator => AddSpectator(player),
            PlayerType.Remote => AddRemotePlayer(player),
            PlayerType.Local => AddLocalPlayer(player),
            _ => throw new ArgumentOutOfRangeException(nameof(player)),
        };

        return result;
    }

    public ResultCode AddLocalPlayer(NetcodePlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (!player.IsLocal())
            return ResultCode.InvalidNetcodePlayer;

        if (addedPlayers.Count >= Max.NumberOfPlayers)
            return ResultCode.TooManyPlayers;

        player.SetQueue(addedPlayers.Count);

        if (!allPlayers.TryAdd(player.Id, player) || !addedPlayers.Add(player))
        {
            player.SetQueue(-1);
            return ResultCode.DuplicatedPlayer;
        }

        endpoints.Add(null);
        IncrementInputBufferSize();
        synchronizer.AddQueue(player);

        return ResultCode.Ok;
    }

    public ResultCode AddRemotePlayer(NetcodePlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (!player.IsRemote() || player.EndPoint is null)
            return ResultCode.InvalidNetcodePlayer;

        if (addedPlayers.Count >= Max.NumberOfPlayers)
            return ResultCode.TooManyPlayers;

        player.SetQueue(addedPlayers.Count);
        if (!allPlayers.TryAdd(player.Id, player) || !addedPlayers.Add(player))
        {
            player.SetQueue(-1);
            return ResultCode.DuplicatedPlayer;
        }

        var protocol = peerConnectionFactory.Create(
            new(player, player.EndPoint, localConnections, syncNumber),
            inputSerializer, peerInputEventQueue, inputComparer
        );

        peerObservers.Add(protocol.GetUdpObserver());
        endpoints.Add(protocol);
        IncrementInputBufferSize();
        synchronizer.AddQueue(player);
        logger.Write(LogLevel.Information, $"Adding {player} at {player.EndPoint}");
        protocol.Synchronize();
        isSynchronizing = true;
        plugins.OnEndpointAdded(this, player.EndPoint, player);
        return ResultCode.Ok;
    }

    public ResultCode AddSpectator(NetcodePlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (!player.IsSpectator() || player.EndPoint is null)
            return ResultCode.InvalidNetcodePlayer;

        if (spectators.Count >= Max.NumberOfSpectators)
            return ResultCode.TooManySpectators;

        // Currently, we can only add spectators before the game starts.
        if (!isSynchronizing)
            return ResultCode.AlreadySynchronized;

        player.SetQueue(spectators.Count);
        if (!allPlayers.TryAdd(player.Id, player) || !addedSpectators.Add(player))
        {
            player.SetQueue(-1);
            return ResultCode.DuplicatedPlayer;
        }

        var protocol = peerConnectionFactory.Create(
            new(player, player.EndPoint, localConnections, syncNumber),
            inputGroupSerializer,
            peerCombinedInputsEventPublisher,
            inputGroupComparer
        );

        peerObservers.Add(protocol.GetUdpObserver());
        spectators.Add(protocol);
        logger.Write(LogLevel.Information, $"Adding {player} at {player.EndPoint}");
        protocol.Synchronize();
        plugins.OnEndpointAdded(this, player.EndPoint, player);
        return ResultCode.Ok;
    }

    public ResultCode AddLocalInput(NetcodePlayer player, in TInput localInput)
    {
        GameInput<TInput> input = new()
        {
            Data = localInput,
        };
        if (isSynchronizing)
            return ResultCode.NotSynchronized;

        if (player.Type is not PlayerType.Local)
            return ResultCode.InvalidNetcodePlayer;

        if (!IsPlayerKnown(player))
            return ResultCode.PlayerOutOfRange;

        if (synchronizer.InRollback)
            return ResultCode.InRollback;

        if (!synchronizer.AddLocalInput(player, ref input))
            return ResultCode.PredictionThreshold;

        // Update the local connect status state to indicate that we've got a confirmed local frame for this player.
        // This must come first so it gets incorporated into the next packet we send.
        if (input.Frame.IsNull)
            return ResultCode.Ok;

        logger.Write(LogLevel.Trace,
            $"setting local connect status for local queue {player.Index} to {input.Frame}");
        localConnections[player].LastFrame = input.Frame;

        // Send the input to all the remote players.
        var sent = true;
        var eps = CollectionsMarshal.AsSpan(endpoints);
        ref var currEp = ref MemoryMarshal.GetReference(eps);
        ref var limitEp = ref Unsafe.Add(ref currEp, eps.Length);

        while (Unsafe.IsAddressLessThan(ref currEp, ref limitEp))
        {
            if (currEp is not null)
            {
                var result = currEp.SendInput(in input);

                if (result is not SendInputResult.Ok)
                {
                    sent = false;
                    logger.Write(LogLevel.Warning,
                        $"Unable to send input to queue {currEp.Player.Index}, {result}");
                }
            }

            currEp = ref Unsafe.Add(ref currEp, 1)!;
        }

        if (!sent)
            return ResultCode.InputDropped;

        return ResultCode.Ok;
    }

    public bool UpdateNetworkStats(NetcodePlayer player)
    {
        var info = player.NetworkStats;

        if (isSynchronizing || player.IsLocal() || !IsPlayerKnown(player))
        {
            info.Valid = false;
            return false;
        }

        endpoints[player.Index]?.GetNetworkStats(ref info);
        info.Valid = true;
        return true;
    }

    [MemberNotNull(nameof(callbacks))]
    public void SetHandler(INetcodeSessionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        callbacks = handler;
        synchronizer.Callbacks = handler;
    }

    public void SetFrameDelay(NetcodePlayer player, int delayInFrames)
    {
        ThrowIf.ArgumentOutOfBounds(player.Index, 0, addedPlayers.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(delayInFrames);
        synchronizer.SetFrameDelay(player, delayInFrames);
    }

    public bool LoadFrame(Frame frame)
    {
        if (frame.Number < 0) return false;
        return synchronizer.TryLoadFrame(in frame);
    }

    public PlayerConnectionStatus GetPlayerStatus(NetcodePlayer player)
    {
        if (!IsPlayerKnown(player)) return PlayerConnectionStatus.Unknown;
        if (player.IsLocal()) return PlayerConnectionStatus.Local;
        if (player.IsSpectator()) return spectators[player.Index].Status.ToPlayerStatus();
        var endpoint = endpoints[player.Index];
        if (endpoint?.IsRunning == true)
            return localConnections.IsConnected(player)
                ? PlayerConnectionStatus.Connected
                : PlayerConnectionStatus.Disconnected;
        return endpoint?.Status.ToPlayerStatus() ?? PlayerConnectionStatus.Unknown;
    }

    public void DisconnectPlayer(NetcodePlayer player)
    {
        if (!IsPlayerKnown(player)) return;
        if (localConnections[player].Disconnected) return;
        if (player.Type is not PlayerType.Remote) return;
        if (endpoints[player.Index] is null)
        {
            var currentFrame = synchronizer.CurrentFrame;
            logger.Write(LogLevel.Information,
                $"Disconnecting {player} at frame {localConnections[player].LastFrame} by user request.");
            for (int i = 0; i < endpoints.Count; i++)
                if (endpoints[i] is { Player: { } epPlayer })
                    DisconnectPlayerQueue(epPlayer, currentFrame);
        }
        else
        {
            logger.Write(LogLevel.Information,
                $"Disconnecting {player} at frame {localConnections[player].LastFrame} by user request.");
            DisconnectPlayerQueue(player, localConnections[player].LastFrame);
        }
    }

    public ref readonly SynchronizedInput<TInput> GetInput(NetcodePlayer player) =>
        ref syncInputBuffer[player.Index];

    public ref readonly SynchronizedInput<TInput> GetInput(int index) =>
        ref syncInputBuffer[index];

    public ReadOnlySpan<SynchronizedInput<TInput>> CurrentSynchronizedInputs => syncInputBuffer;

    public ReadOnlySpan<TInput> CurrentInputs => inputBuffer;

    public void AdvanceFrame()
    {
        logger.Write(LogLevel.Trace, $"[End Frame {synchronizer.CurrentFrame}]");
        synchronizer.IncrementFrame();
    }

    public ResultCode SynchronizeInputs()
    {
        if (isSynchronizing)
            return ResultCode.NotSynchronized;
        synchronizer.SynchronizeInputs(syncInputBuffer, inputBuffer);
        random.UpdateSeed(CurrentFrame, inputBuffer, extraSeedState);
        return ResultCode.Ok;
    }

    uint extraSeedState;
    public void SetRandomSeed(uint seed, uint extraState = 0) => extraSeedState = unchecked(seed + extraState);

    bool IsPlayerKnown(NetcodePlayer player) =>
        player.Index >= 0
        && player.Index <
        player.Type switch
        {
            PlayerType.Remote => endpoints.Count,
            PlayerType.Spectator => spectators.Count,
            _ => int.MaxValue,
        };

    void IncrementInputBufferSize()
    {
        Array.Resize(ref syncInputBuffer, syncInputBuffer.Length + 1);
        Array.Resize(ref inputBuffer, syncInputBuffer.Length);
    }

    void CheckInitialSync()
    {
        if (!isSynchronizing) return;

        // Check to see if everyone is now synchronized.  If so,
        // go ahead and tell the client that we're ok to accept input.
        var endpointsSpan = CollectionsMarshal.AsSpan(endpoints);

        for (var i = 0; i < endpointsSpan.Length; i++)
            if (endpointsSpan[i] is { IsRunning: false } ep && localConnections.IsConnected(ep.Player))
                return;

        var spectatorsSpan = CollectionsMarshal.AsSpan(spectators);
        for (var i = 0; i < spectatorsSpan.Length; i++)
            if (spectatorsSpan[i] is { IsRunning: false, Status: not ProtocolStatus.Disconnected })
                return;

        for (var i = 0; i < endpointsSpan.Length; i++) endpointsSpan[i]?.Start();
        for (var i = 0; i < spectatorsSpan.Length; i++) spectatorsSpan[i].Start();

        isSynchronizing = false;
        callbacks.OnSessionStart();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Span<PeerConnection<TInput>?> UpdateEndpoints()
    {
        var span = CollectionsMarshal.AsSpan(endpoints);
        ref var curr = ref MemoryMarshal.GetReference(span);
        ref var limit = ref Unsafe.Add(ref curr, span.Length);
        while (Unsafe.IsAddressLessThan(ref curr, ref limit))
        {
            curr?.Update();
            curr = ref Unsafe.Add(ref curr, 1)!;
        }

        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Span<PeerConnection<ConfirmedInputs<TInput>>> UpdateSpectators()
    {
        var span = CollectionsMarshal.AsSpan(spectators);
        ref var curr = ref MemoryMarshal.GetReference(span);
        ref var limit = ref Unsafe.Add(ref curr, span.Length);
        while (Unsafe.IsAddressLessThan(ref curr, ref limit))
        {
            curr.Update();
            curr = ref Unsafe.Add(ref curr, 1)!;
        }

        return span;
    }

    void DoSync()
    {
        plugins.OnFrameBegin(this, isSynchronizing);
        udp.Socket.Update();
        synchronizer.UpdateRollbackFrameCounter();
        ConsumeProtocolNetworkEvents();
        jobManager.ThrowIfError();

        if (synchronizer.InRollback) return;
        ConsumeProtocolInputEvents();

        var eps = UpdateEndpoints();
        var specs = UpdateSpectators();

        if (isSynchronizing) return;

        synchronizer.CheckSimulation();

        // notify all of our endpoints of their local frame number for their next connection quality report
        int i;
        var currentFrame = synchronizer.CurrentFrame;
        for (i = 0; i < eps.Length; i++)
            eps[i]?.SetLocalFrameNumber(currentFrame, FixedFrameRate);

        var minConfirmedFrame = NumberOfPlayers <= 2 ? MinimumFrame2Players() : MinimumFrameNPlayers();
        ThrowIf.Assert(minConfirmedFrame != Frame.MaxValue);
        logger.Write(LogLevel.Trace, $"last confirmed frame in p2p backend is {minConfirmedFrame}");

        if (minConfirmedFrame >= Frame.Zero)
        {
            if (NumberOfSpectators > 0)
            {
                GameInput<ConfirmedInputs<TInput>> confirmed = new(nextSpectatorFrame);
                while (nextSpectatorFrame <= minConfirmedFrame)
                {
                    if (!synchronizer.GetConfirmedInputGroup(in nextSpectatorFrame, ref confirmed))
                        break;

                    logger.Write(LogLevel.Trace, $"pushing frame {nextSpectatorFrame} to spectators");
                    for (var s = 0; s < specs.Length; s++)
                        if (specs[s].IsRunning)
                            specs[s].SendInput(in confirmed);

                    nextSpectatorFrame++;
                }
            }

            if (inputListener is not null)
            {
                GameInput<ConfirmedInputs<TInput>> confirmed = new(nextListenerFrame);
                while (nextListenerFrame <= minConfirmedFrame)
                {
                    if (!synchronizer.GetConfirmedInputGroup(in nextListenerFrame, ref confirmed))
                        break;

                    logger.Write(LogLevel.Trace, $"pushing frame {nextListenerFrame} to listener");
                    var inputs = confirmed.Data.Inputs[..confirmed.Data.Count];
                    inputListener.OnConfirmed(in confirmed.Frame, inputs);

                    nextListenerFrame++;
                }
            }

            logger.Write(LogLevel.Trace, $"setting confirmed frame in sync to {minConfirmedFrame}");
            synchronizer.SetLastConfirmedFrame(minConfirmedFrame);
        }

        // send time sync notifications if now is the proper time
        if (currentFrame.Number <= nextRecommendedInterval)
            return;

        var interval = 0;
        for (i = 0; i < eps.Length; i++)
            if (eps[i] is { } endpoint)
                interval = Math.Max(interval, endpoint.GetRecommendFrameDelay());

        if (interval <= 0) return;
        callbacks.TimeSync(new(interval));
        nextRecommendedInterval = currentFrame.Number + options.RecommendationInterval;
    }

    void ConsumeProtocolInputEvents()
    {
        while (peerInputEventQueue.TryConsume(out var gameInputEvent))
        {
            var (player, eventInput) = gameInputEvent;
            if (!player.IsRemote())
            {
                logger.Write(LogLevel.Warning, $"non-remote input received from {player}");
                continue;
            }

            if (localConnections[player].Disconnected)
                continue;

            var currentRemoteFrame = localConnections[player].LastFrame;
            var newRemoteFrame = eventInput.Frame;

            ThrowIf.Assert(currentRemoteFrame.IsNull || newRemoteFrame == currentRemoteFrame.Next());
            synchronizer.AddRemoteInput(player, eventInput);
            // Notify the other endpoints which frame we received from a peer
            logger.Write(LogLevel.Trace, $"setting remote connect status frame {player} to {eventInput.Frame}");
            localConnections[player].LastFrame = eventInput.Frame;
        }
    }

    void ConsumeProtocolNetworkEvents()
    {
        while (networkEventQueue.TryRead(out var evt))
            OnNetworkEvent(in evt);

        CheckInitialSync();
    }

    void RemoveSpectator(NetcodePlayer player)
    {
        spectators[player.Index].Disconnect();
        addedSpectators.Remove(player);
        allPlayers.Remove(player.Id);
    }

    void OnNetworkEvent(in ProtocolEventInfo evt)
    {
        ref readonly var player = ref evt.Player;
        logger.Write(LogLevel.Trace, $"Session event: {evt} from {player}");
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
                break;
            case ProtocolEvent.SyncFailure:
                if (player.IsSpectator())
                    RemoveSpectator(player);
                else
                    callbacks.OnPeerEvent(player, new(PeerEvent.SynchronizationFailure));

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
                switch (player.Type)
                {
                    case PlayerType.Spectator:
                        RemoveSpectator(player);
                        break;
                    case PlayerType.Remote:
                        DisconnectPlayer(player);
                        break;
                }

                callbacks.OnPeerEvent(player, new(PeerEvent.Disconnected));
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
        var eps = CollectionsMarshal.AsSpan(endpoints);
        for (var i = 0; i < eps.Length; i++)
        {
            var queueConnected = true;
            if (eps[i] is { IsRunning: true } endpoint)
                queueConnected = endpoint.GetPeerConnectStatus(i, out _);

            ref var localConn = ref localConnections[i];

            if (!localConn.Disconnected)
                totalMinConfirmed = Frame.Min(in localConnections[i].LastFrame, in totalMinConfirmed);

            logger.Write(LogLevel.Trace,
                $"Queue {i} => connected: {!localConn.Disconnected}; last received: {localConn.LastFrame}; min confirmed: {totalMinConfirmed}");

            if (!queueConnected && !localConn.Disconnected && eps[i] is { Player: var handler })
            {
                logger.Write(LogLevel.Information, $"disconnecting {i} by remote request");
                DisconnectPlayerQueue(handler, in totalMinConfirmed);
            }

            logger.Write(LogLevel.Trace, $"Queue {i} => min confirmed = {totalMinConfirmed}");
        }

        return totalMinConfirmed;
    }

    Frame MinimumFrameNPlayers()
    {
        // discard confirmed frames as appropriate
        var totalMinConfirmed = Frame.MaxValue;
        var eps = CollectionsMarshal.AsSpan(endpoints);
        for (var queue = 0; queue < NumberOfPlayers; queue++)
        {
            var queueConnected = true;
            var queueMinConfirmed = Frame.MaxValue;
            logger.Write(LogLevel.Trace, $"considering queue {queue}");
            for (var i = 0; i < eps.Length; i++)
            {
                // we're going to do a lot of logic here in consideration of endpoint i.
                // keep accumulating the minimum confirmed point for all n*n packets and
                // throw away the rest.
                if (eps[i] is { IsRunning: true } endpoint)
                {
                    var connected = endpoint.GetPeerConnectStatus(queue, out var lastReceived);
                    queueConnected = queueConnected && connected;
                    queueMinConfirmed = Frame.Min(in lastReceived, in queueMinConfirmed);
                    logger.Write(LogLevel.Trace,
                        $"Queue {i} => connected: {connected}; last received: {lastReceived}; min confirmed: {queueMinConfirmed}");
                }
                else
                    logger.Write(LogLevel.Trace, $"Queue {i}: ignoring... not running.");
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
                if ((!localStatus.Disconnected || localStatus.LastFrame > queueMinConfirmed)
                    && eps[queue] is { Player: var handle })
                {
                    logger.Write(LogLevel.Information, $"disconnecting queue {queue} by remote request");
                    DisconnectPlayerQueue(handle, in queueMinConfirmed);
                }
            }

            logger.Write(LogLevel.Trace, $"[Endpoint {queue}] total min confirmed = {totalMinConfirmed}");
        }

        return totalMinConfirmed;
    }

    void DisconnectPlayerQueue(NetcodePlayer player, in Frame syncTo)
    {
        var frameCount = synchronizer.CurrentFrame;
        endpoints[player.Index]?.Disconnect();
        ref var connStatus = ref localConnections[player];
        logger.Write(LogLevel.Debug,
            $"Changing player {player} local connect status for last frame from {connStatus.LastFrame.Number} to {syncTo} on disconnect request (current: {frameCount})");
        connStatus.Disconnected = true;
        connStatus.LastFrame = syncTo;
        if (syncTo < frameCount && !syncTo.IsNull)
        {
            logger.Write(LogLevel.Information,
                $"adjusting simulation to account for the fact that {player} disconnected on frame {syncTo}");
            synchronizer.AdjustSimulation(in syncTo);
            logger.Write(LogLevel.Information, "finished adjusting simulation.");
        }

        callbacks.OnPeerEvent(player, new(PeerEvent.Disconnected));
        CheckInitialSync();
    }
}
