using System.Net;
using nGGPO.Core;
using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network;
using nGGPO.Network.Client;
using nGGPO.Network.Messages;
using nGGPO.Network.Protocol;
using nGGPO.Serialization;

namespace nGGPO.Backends;

sealed class Peer2PeerBackend<TInput, TGameState> : IRollbackSession<TInput>
    where TInput : struct
    where TGameState : struct
{
    readonly IBinarySerializer<TInput> inputSerializer;
    readonly IRollbackHandler<TGameState> callbacks;

    readonly IUdpClient<ProtocolMessage> udp;
    readonly UdpObserverGroup<ProtocolMessage> udpObservers;
    readonly Synchronizer<TGameState> synchronizer;
    readonly ConnectionStatuses localConnections;

    bool synchronizing = true;

    readonly List<PeerConnection> spectators;
    readonly List<PeerConnection> endpoints;
    readonly IBackgroundJobManager backgroundJobManager;
    readonly ILogger logger;

    readonly RollbackOptions options;

    readonly GameInput emptyGameInput = GameInput.CreateEmpty();

    public Peer2PeerBackend(
        RollbackOptions options,
        IRollbackHandler<TGameState> callbacks,
        IBinarySerializer<TInput> inputSerializer,
        IUdpClientFactory udpClientFactory,
        IBackgroundJobManager backgroundJobManager,
        ILogger logger
    )
    {
        ThrowHelpers.ThrowIfArgumentIsNegativeOrZero(options.LocalPort);
        ThrowHelpers.ThrowIfArgumentIsNegativeOrZero(options.NumberOfPlayers);
        ThrowHelpers.ThrowIfTypeTooBigForStack<GameInput>();
        ThrowHelpers.ThrowIfTypeSizeGreaterThan<GameInput>(Max.InputBytes * Max.InputPlayers * 2);
        ThrowHelpers.ThrowIfTypeTooBigForStack<TInput>();

        var inputTypeSize = inputSerializer.GetTypeSize();
        ThrowHelpers.ThrowIfArgumentOutOfBounds(inputTypeSize, 1, Max.InputBytes);

        this.options = options;
        this.inputSerializer = inputSerializer;
        this.callbacks = callbacks;
        this.backgroundJobManager = backgroundJobManager;
        this.logger = logger;

        this.options.InputSize = inputTypeSize;

        localConnections = new();
        synchronizer = new(this.options, this.logger, localConnections);
        spectators = new(options.NumberOfSpectators);
        endpoints = new(this.options.NumberOfPlayers);
        udpObservers = new();
        udp = udpClientFactory.CreateClient(
            this.options.LocalPort,
            this.options.EnableEndianness,
            this.options.UdpPacketBufferSize,
            udpObservers,
            this.logger
        );

        backgroundJobManager.Register(udp);
    }

    public async ValueTask DisposeAsync()
    {
        udp.Dispose();
        await backgroundJobManager.DisposeAsync().ConfigureAwait(false);

        foreach (var endpoint in endpoints)
            endpoint.Dispose();

        foreach (var spectator in spectators)
            spectator.Dispose();
    }

    public async ValueTask<ResultCode> AddPlayer(Player player, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (player is Player.Spectator spectator)
            return await AddSpectator(spectator.EndPoint, ct);

        if (player.PlayerNumber < 1 || player.PlayerNumber > options.NumberOfPlayers)
            return ResultCode.PlayerOutOfRange;

        if (player is Player.Remote remote)
            await AddRemotePlayer(remote.EndPoint, player.QueueNumber, ct);

        return ResultCode.Ok;
    }

    public ResultCode SetFrameDelay(Player player, int delayInFrames)
    {
        ArgumentNullException.ThrowIfNull(player);

        var result = PlayerIdToQueue(player.Id, out var queue);

        if (result.IsFailure())
            return result;

        synchronizer.SetFrameDelay(queue, delayInFrames);

        return ResultCode.Ok;
    }

    public ResultCode TryAddLocalInput(PlayerId player, TInput localInput)
    {
        var localInputResult = CreateGameInput(player, localInput, out var input);
        if (localInputResult.IsFailure())
            return localInputResult;

        // Send the input to all the remote players.
        var allSent = true;
        var anySent = false;
        for (var i = 0; i < endpoints.Count; i++)
        {
            var sent = endpoints[i].TrySendInput(in input);
            allSent = allSent && sent;
            anySent = anySent || sent;
        }

        if (!allSent && anySent)
            return ResultCode.InputPartiallyDropped;

        if (!allSent)
            return ResultCode.InputDropped;

        return ResultCode.Ok;
    }

    public async ValueTask<ResultCode> AddLocalInput(
        PlayerId player,
        TInput localInput,
        CancellationToken stoppingToken = default)
    {
        var localInputResult = CreateGameInput(player, localInput, out var input);
        if (localInputResult.IsFailure())
            return localInputResult;

        // Send the input to all the remote players.
        for (var i = 0; i < endpoints.Count; i++)
            await endpoints[i].SendInput(in input, stoppingToken).ConfigureAwait(false);

        return ResultCode.Ok;
    }

    ResultCode CreateGameInput(PlayerId player, TInput localInput, out GameInput input)
    {
        if (synchronizer.InRollback())
        {
            input = emptyGameInput;
            return ResultCode.InRollback;
        }

        if (synchronizing)
        {
            input = emptyGameInput;
            return ResultCode.NotSynchronized;
        }

        var result = PlayerIdToQueue(player, out var queue);
        if (result.IsSuccess())
        {
            input = emptyGameInput;
            return result;
        }

        GameInputBuffer buffer = new();
        var size = inputSerializer.Serialize(ref localInput, buffer);
        Tracer.Assert(size == options.InputSize);
        input = new(in buffer, size);

        if (!synchronizer.AddLocalInput(queue, input))
            return ResultCode.PredictionThreshold;

        if (input.Frame.IsNull)
            return ResultCode.Ok;

        logger.Info($"setting local connect status for local queue {queue} to {input.Frame}");

        localConnections[queue].LastFrame = input.Frame;
        return ResultCode.Ok;
    }

    public ResultCode SynchronizeInputs(params TInput[] inputs) => SynchronizeInputs(out _, inputs);

    public ResultCode SynchronizeInputs(out Span<int> disconnectFlags, params TInput[] inputs)
    {
        if (synchronizing)
        {
            disconnectFlags = [];
            return ResultCode.NotSynchronized;
        }

        disconnectFlags = synchronizer.SynchronizeInputs(inputs);
        return ResultCode.Ok;
    }

    ResultCode PlayerIdToQueue(in PlayerId player, out QueueIndex queue)
    {
        var offset = player.QueueNumber;
        if (offset.Value < 0 || offset.Value >= options.NumberOfPlayers)
        {
            queue = PlayerId.Empty.QueueNumber;
            return ResultCode.InvalidPlayerHandle;
        }

        queue = offset;
        return ResultCode.Ok;
    }

    PeerConnection CreateProtocol(IPEndPoint endpoint, QueueIndex queue)
    {
        ProtocolOptions protocolOptions = new()
        {
            Queue = queue,
            DisconnectTimeout = options.DisconnectTimeout,
            DisconnectNotifyStart = options.DisconnectNotifyStart,
            NetworkDelay = options.NetworkDelay,
            UdpPacketBufferSize = options.UdpPacketBufferSize,
            Peer = endpoint,
        };

        var connection = PeerConnectionFactory.CreateDefault(
            options.Random,
            logger,
            backgroundJobManager,
            udp,
            localConnections,
            protocolOptions,
            options.TimeSync
        );

        udpObservers.Add(connection.GetUdpObserver());

        return connection;
    }

    async ValueTask AddRemotePlayer(IPEndPoint endpoint, QueueIndex queue, CancellationToken ct)
    {
        /*
         * Start the state machine (xxx: no)
         */
        synchronizing = true;
        var protocol = CreateProtocol(endpoint, queue);
        await protocol.Synchronize(ct);
        endpoints.Add(protocol);
    }

    async ValueTask<ResultCode> AddSpectator(IPEndPoint endpoint, CancellationToken ct)
    {
        var numSpectators = spectators.Count;

        if (numSpectators >= Max.Spectators)
            return ResultCode.TooManySpectators;

        /*
         * Currently, we can only add spectators before the game starts.
         */
        if (!synchronizing)
            return ResultCode.InvalidRequest;

        QueueIndex queue = new(options.SpectatorOffset + numSpectators + 1);
        var protocol = CreateProtocol(endpoint, queue);
        await protocol.Synchronize(ct);
        spectators.Add(protocol);

        return ResultCode.Ok;
    }

    public Task Start(CancellationToken ct = default) => backgroundJobManager.Start(ct);

    PlayerId QueueToSpectatorHandle(int queue) =>
        new(queue + options.SpectatorOffset); /* out of range of the player array, basically */
}
