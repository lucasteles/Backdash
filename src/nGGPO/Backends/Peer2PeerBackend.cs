using System.Net;
using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Lifecycle;
using nGGPO.Network;
using nGGPO.Network.Client;
using nGGPO.Network.Messages;
using nGGPO.Network.Protocol;
using nGGPO.Serialization;
using nGGPO.Utils;

namespace nGGPO.Backends;

sealed class Peer2PeerBackend<TInput, TGameState>
    : IRollbackSession<TInput, TGameState>
    where TInput : struct
    where TGameState : struct
{
    readonly IBinarySerializer<TInput> inputSerializer;
    readonly ISessionCallbacks<TGameState> callbacks;

    readonly UdpClient<ProtocolMessage> udpClient;
    readonly Synchronizer<TGameState> sync;
    readonly Connections localConnections;

    bool synchronizing = true;

    readonly List<UdpProtocol> spectators;
    readonly List<UdpProtocol> endpoints;
    readonly UdpObserverGroup<ProtocolMessage> peerObservers = new();
    readonly BackgroundJobManager backgroundJobManager = new();
    readonly RollbackOptions options;

    public Peer2PeerBackend(
        IBinarySerializer<TInput> inputSerializer,
        ISessionCallbacks<TGameState> callbacks,
        RollbackOptions options,
        IBinarySerializer<ProtocolMessage>? udpMsgSerializer = null
    )
    {
        ExceptionHelper.ThrowIfArgumentIsNegativeOrZero(options.LocalPort);
        ExceptionHelper.ThrowIfArgumentIsNegativeOrZero(options.NumberOfPlayers);

        this.options = options;
        this.inputSerializer = inputSerializer;
        this.callbacks = callbacks;

        localConnections = new();
        spectators = new(Max.Spectators);
        endpoints = new(this.options.NumberOfPlayers);
        sync = new(localConnections);
        udpClient = new(this.options.LocalPort, peerObservers,
            udpMsgSerializer ?? new ProtocolMessageBinarySerializer());

        backgroundJobManager.Register(udpClient);
    }

    public void Dispose() => udpClient.Dispose();

    public ResultCode AddPlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (player is Player.Spectator spectator)
            return AddSpectator(spectator.EndPoint);

        if (player.PlayerNumber < 1 || player.PlayerNumber > options.NumberOfPlayers)
            return ResultCode.PlayerOutOfRange;

        if (player is Player.Remote remote)
            AddRemotePlayer(remote.EndPoint, player.QueueNumber);

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

    GameInput ParseInput(ref TInput input)
    {
        GameInputBuffer buffer = new();
        var size = inputSerializer.Serialize(ref input, buffer);
        return new GameInput(in buffer, size);
    }

    public ValueTask<ResultCode> AddLocalInput(Player player, TInput localInput,
        CancellationToken stoppingToken = default)
        => AddLocalInput(player.Id, localInput, stoppingToken);

    public async ValueTask<ResultCode> AddLocalInput(PlayerId player, TInput localInput,
        CancellationToken stoppingToken = default)
    {
        if (sync.InRollback())
            return ResultCode.InRollback;

        if (synchronizing)
            return ResultCode.NotSynchronized;

        var result = PlayerIdToQueue(player, out var queue);
        if (!result.IsSuccess())
            return result;

        var input = ParseInput(ref localInput);

        if (!sync.AddLocalInput(queue, input))
            return ResultCode.PredictionThreshold;

        if (input.Frame.IsNull) return ResultCode.Ok;

        Tracer.Log("setting local connect status for local queue {0} to {1}",
            queue, input.Frame);

        localConnections[queue].LastFrame = input.Frame;

        // Send the input to all the remote players.
        for (var i = 0; i < options.NumberOfPlayers; i++)
            if (endpoints[i].IsInitialized())
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

    UdpProtocol CreateProtocol(IPEndPoint endpoint, QueueIndex queue)
    {
        ProtocolOptions protocolOptions = new()
        {
            Random = options.Random,
            Queue = queue,
            DisconnectTimeout = options.DisconnectTimeout,
            DisconnectNotifyStart = options.DisconnectNotifyStart,
            NetworkDelay = options.NetworkDelay,
            Peer = endpoint,
        };

        var protocol = UdpProtocolFactory.CreateDefault(
            backgroundJobManager,
            peerObservers,
            protocolOptions,
            udpClient,
            localConnections
        );

        protocol.Synchronize();
        return protocol;
    }

    void AddRemotePlayer(IPEndPoint endpoint, QueueIndex queue)
    {
        /*
         * Start the state machine (xxx: no)
         */
        synchronizing = true;

        var protocol = CreateProtocol(endpoint, queue);
        endpoints.Add(protocol);
    }

    ResultCode AddSpectator(IPEndPoint endpoint)
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
        spectators.Add(protocol);

        return ResultCode.Ok;
    }

    public Task Start(CancellationToken ct = default) => backgroundJobManager.Start(ct);

    PlayerId QueueToSpectatorHandle(int queue) =>
        new(queue + options.SpectatorOffset); /* out of range of the player array, basically */
}
