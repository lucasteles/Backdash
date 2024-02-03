using System.Net;
using System.Runtime.CompilerServices;
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
    readonly ISessionCallbacks<TGameState> callbacks;

    readonly IUdpObservableClient<ProtocolMessage> udp;
    readonly Synchronizer<TGameState> sync;
    readonly ConnectionStatuses localConnections;

    bool synchronizing = true;

    readonly List<PeerConnection> spectators;
    readonly List<PeerConnection> endpoints;
    readonly IBackgroundJobManager backgroundJobManager;
    readonly ILogger logger;

    readonly RollbackOptions options;

    public Peer2PeerBackend(
        RollbackOptions options,
        ISessionCallbacks<TGameState> callbacks,
        IBinarySerializer<TInput> inputSerializer,
        IUdpObservableClient<ProtocolMessage> udpClient,
        IBackgroundJobManager backgroundJobManager,
        ILogger logger
    )
    {
        ExceptionHelper.ThrowIfArgumentIsNegativeOrZero(options.LocalPort);
        ExceptionHelper.ThrowIfArgumentIsNegativeOrZero(options.NumberOfPlayers);

        if (!Mem.IsValidSizeOnStack<GameInput>(Max.InputBytes * Max.InputPlayers * 2))
            throw new NggpoException($"{nameof(GameInput)} size too big: {Unsafe.SizeOf<GameInput>()}");

        this.options = options;
        this.inputSerializer = inputSerializer;
        this.callbacks = callbacks;
        this.backgroundJobManager = backgroundJobManager;
        this.logger = logger;

        localConnections = new();
        sync = new(localConnections);
        spectators = new(Max.Spectators);
        endpoints = new(this.options.NumberOfPlayers);
        udp = udpClient;

        backgroundJobManager.Register(udp.Client);
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

        sync.SetFrameDelay(queue, delayInFrames);

        return ResultCode.Ok;
    }

    public ValueTask<ResultCode> AddLocalInput(
        Player player,
        TInput localInput,
        CancellationToken stoppingToken = default
    ) => AddLocalInput(player.Id, localInput, stoppingToken);

    public async ValueTask<ResultCode> AddLocalInput(
        PlayerId player,
        TInput localInput,
        CancellationToken stoppingToken = default)
    {
        if (sync.InRollback())
            return ResultCode.InRollback;

        if (synchronizing)
            return ResultCode.NotSynchronized;

        var result = PlayerIdToQueue(player, out var queue);
        if (!result.IsSuccess())
            return result;

        GameInputBuffer buffer = new();
        var size = inputSerializer.Serialize(ref localInput, buffer);
        GameInput input = new(in buffer, size);

        if (!sync.AddLocalInput(queue, input))
            return ResultCode.PredictionThreshold;

        if (input.Frame.IsNull) return ResultCode.Ok;

        logger.Info($"setting local connect status for local queue {queue} to {input.Frame}");

        localConnections[queue].LastFrame = input.Frame;

        // Send the input to all the remote players.
        for (var i = 0; i < endpoints.Count; i++)
            await endpoints[i].SendInput(in input, stoppingToken).ConfigureAwait(false);

        return ResultCode.Ok;
    }

    public ResultCode SynchronizeInputs(params TInput[] inputs) => SynchronizeInputs(out _, inputs);

    public ResultCode SynchronizeInputs(out int[] disconnectFlags, params TInput[] inputs)
    {
        if (synchronizing)
        {
            disconnectFlags = [];
            return ResultCode.NotSynchronized;
        }

        disconnectFlags = sync.SynchronizeInputs(inputs);
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
            Peer = endpoint,
        };

        return PeerConnectionFactory.CreateDefault(
            options.Random,
            logger,
            backgroundJobManager,
            udp,
            localConnections,
            protocolOptions,
            options.TimeSync
        );
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
