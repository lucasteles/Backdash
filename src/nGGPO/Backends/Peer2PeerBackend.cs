using System.Net;
using nGGPO.Input;
using nGGPO.Network;
using nGGPO.Network.Messages;
using nGGPO.Serialization;
using nGGPO.Utils;
using UdpProtocol = nGGPO.Network.Protocol.UdpProtocol;

namespace nGGPO.Backends;

sealed class Peer2PeerBackend<TInput, TGameState> : IRollbackSession<TInput, TGameState>
    where TInput : struct
    where TGameState : struct
{
    const int RecommendationInterval = 240;
    const int DefaultDisconnectTimeout = 5000;
    const int DefaultDisconnectNotifyStart = 750;
    const int SpectatorOffset = 1000;

    readonly IBinarySerializer<TInput> inputSerializer;
    readonly ISessionCallbacks<TGameState> callbacks;

    readonly UdpPeerClient<UdpMsg> udp;
    readonly Synchronizer<TGameState> sync;
    readonly ConnectStatus[] localConnectStatus = new ConnectStatus[Max.MsgPlayers];

    readonly int numPlayers;
    int numSpectators;
    bool synchronizing = true;

    readonly List<UdpProtocol> spectators = new(Max.Spectators);
    readonly List<UdpProtocol> endpoints;

    readonly int disconnectTimeout = DefaultDisconnectTimeout;
    readonly int disconnectNotifyStart = DefaultDisconnectNotifyStart;

    public Peer2PeerBackend(
        IBinarySerializer<TInput> inputSerializer,
        ISessionCallbacks<TGameState> callbacks,
        int localPort,
        int numPlayers,
        IBinarySerializer<UdpMsg>? udpMsgSerializer = null
    )
    {
        ExceptionHelper.ThrowIfArgumentIsNegativeOrZero(localPort);
        ExceptionHelper.ThrowIfArgumentIsNegativeOrZero(numPlayers);

        this.inputSerializer = inputSerializer;
        this.callbacks = callbacks;
        this.numPlayers = numPlayers;

        endpoints = new(numPlayers);
        sync = new(localConnectStatus);
        udp = new(localPort, udpMsgSerializer ?? new UdpMsgBinarySerializer());
    }

    public ResultCode AddPlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (player is Player.Spectator spectator)
            return AddSpectator(spectator.EndPoint);

        if (player.PlayerNumber < 1 || player.PlayerNumber > numPlayers)
            return ResultCode.PlayerOutOfRange;

        var queue = player.PlayerNumber - 1;
        player.SetHandle(QueueToPlayerHandle(queue));

        if (player is Player.Remote remote)
            AddRemotePlayer(remote.EndPoint, queue);

        return ResultCode.Ok;
    }

    public ResultCode SetFrameDelay(Player player, int delayInFrames)
    {
        var result = PlayerHandleToQueue(player.Handle, out var queue);

        if (result is not ResultCode.Ok)
            return result;

        sync.SetFrameDelay(queue, delayInFrames);

        return ResultCode.Ok;
    }

    GameInput ParseInput(ref TInput input)
    {
        GameInputBuffer buffer = new();
        var size = inputSerializer.Serialize(ref input, buffer);
        return new GameInput(ref buffer, size);
    }

    public ValueTask<ResultCode> AddLocalInput(Player player, TInput localInput) =>
        AddLocalInput(player.Handle, localInput);

    public async ValueTask<ResultCode> AddLocalInput(PlayerHandle player, TInput localInput)
    {
        if (sync.InRollback())
            return ResultCode.InRollback;

        if (synchronizing)
            return ResultCode.NotSynchronized;

        var result = PlayerHandleToQueue(player, out var queue);
        if (!result.IsSuccess())
            return result;

        var input = ParseInput(ref localInput);

        if (!sync.AddLocalInput(queue, input))
            return ResultCode.PredictionThreshold;

        if (input.Frame.IsNull) return ResultCode.Ok;

        Tracer.Log("setting local connect status for local queue {0} to {1}",
            queue, input.Frame);

        localConnectStatus[queue].LastFrame = input.Frame;

        // Send the input to all the remote players.
        for (var i = 0; i < numPlayers; i++)
            if (endpoints[i].IsInitialized())
                await endpoints[i].SendInput(in input).ConfigureAwait(false);

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

    ResultCode PlayerHandleToQueue(in PlayerHandle player, out int queue)
    {
        var offset = player.Value - 1;
        if (offset < 0 || offset >= numPlayers)
        {
            queue = PlayerHandle.Empty.Value;
            return ResultCode.InvalidPlayerHandle;
        }

        queue = offset;
        return ResultCode.Ok;
    }

    static PlayerHandle QueueToPlayerHandle(int queue) => new(queue + 1);

    static PlayerHandle QueueToSpectatorHandle(int queue) =>
        new(queue + SpectatorOffset); /* out of range of the player array, basically */

    UdpProtocol CreateProtocol(IPEndPoint endpoint, int queue)
    {
        UdpProtocol protocol = new(
            timeSync: new(),
            random: Rnd.Shared,
            udp: udp,
            queue,
            endpoint,
            localConnectStatus
        )
        {
            DisconnectTimeout = disconnectTimeout,
            DisconnectNotifyStart = disconnectNotifyStart,
        };

        protocol.Synchronize();
        return protocol;
    }

    void AddRemotePlayer(IPEndPoint endpoint, int queue)
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
        if (numSpectators == Max.Spectators)
            return ResultCode.TooManySpectators;

        /*
         * Currently, we can only add spectators before the game starts.
         */
        if (!synchronizing)
            return ResultCode.InvalidRequest;

        var queue = numSpectators++;
        var protocol = CreateProtocol(endpoint, queue + SpectatorOffset);
        spectators.Add(protocol);

        return ResultCode.Ok;
    }

    public Task StartPumping(CancellationToken ct = default)
    {
        var tasks = endpoints
            .Select(e => e.StartPumping(ct))
            .Append(udp.StartPumping(ct));

        return Task.Run(
            async () => await Task.WhenAll(tasks).ConfigureAwait(false)
            , ct
        );
    }

    public void Dispose() => udp.Dispose();
}
