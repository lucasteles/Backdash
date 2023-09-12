using System;
using System.Collections.Generic;
using System.Net;
using nGGPO.Network;
using nGGPO.Network.Messages;
using nGGPO.Types;

namespace nGGPO.Backends;

class Peer2PeerBackend<TInput, TGameState> : ISession<TInput, TGameState>
    where TInput : struct
    where TGameState : struct
{
    const int RecommendationInterval = 240;
    const int DefaultDisconnectTimeout = 5000;
    const int DefaultDisconnectNotifyStart = 750;
    const int SpectatorOffset = 1000;

    readonly IBinaryEncoder encoder;
    readonly ISessionCallbacks<TGameState> callbacks;

    readonly Poll poll;
    readonly Udp udp;
    readonly Sync sync;
    readonly StaticBuffer<ConnectStatus> localConnectStatus = new(Max.UdpMsgPlayers);

    int numPlayers;
    int numSpectators;
    bool synchronizing = true;

    readonly List<UdpProtocol> spectators = new(Max.Spectators);
    readonly List<UdpProtocol> endpoints;

    int disconnectTimeout = DefaultDisconnectTimeout;
    int disconnectNotifyStart = DefaultDisconnectNotifyStart;

    public Peer2PeerBackend(
        IBinaryEncoder encoder,
        ISessionCallbacks<TGameState> callbacks,
        string gameName,
        int localport,
        int numPlayers
    )
    {
        this.encoder = encoder;
        this.callbacks = callbacks;
        this.numPlayers = numPlayers;

        endpoints = new(numPlayers);
        sync = new(localConnectStatus);
        poll = new();

        udp = new(encoder, localport);
        udp.OnMsg += OnMsg;
        poll.RegisterLoop(udp);

        callbacks.BeginGame(gameName);
    }

    public ErrorCode AddPlayer(Player player)
    {
        if (player is null) throw new ArgumentNullException(nameof(player));

        if (player is Player.Spectator spectator)
            return AddSpectator(spectator.EndPoint);

        if (player.PlayerNumber < 1 || player.PlayerNumber > numPlayers)
            return ErrorCode.PlayerOutOfRange;

        var queue = player.PlayerNumber - 1;
        player.SetHandle(QueueToPlayerHandle(queue));

        if (player is Player.Remote remote)
            AddRemotePlayer(remote.EndPoint, queue);

        return ErrorCode.Ok;
    }

    public ErrorCode SetFrameDelay(Player player, int delay)
    {
        var result = PlayerHandleToQueue(player.Handle, out var queue);

        if (result is not ErrorCode.Ok)
            return result;

        sync.SetFrameDelay(queue, delay);

        return ErrorCode.Ok;
    }

    public ErrorCode AddLocalInput(Player player, TInput input) =>
        AddLocalInput(player.Handle, input);

    public ErrorCode AddLocalInput(PlayerHandle player, TInput localInput)
    {
        if (sync.InRollback())
            return ErrorCode.InRollback;

        if (synchronizing)
            return ErrorCode.NotSynchronized;

        var result = PlayerHandleToQueue(player, out var queue);
        if (!result.IsSuccess())
            return result;

        var inputBytes = encoder.Encode(localInput);
        GameInput input = new();

        if (!sync.AddLocalInput(queue, input))
            return ErrorCode.PredictionThreshold;

        if (input.Frame is not GameInput.NullFrame)
        {
            Logger.Info("setting local connect status for local queue {0} to {1}",
                queue, input.Frame);

            localConnectStatus.Ref(queue).LastFrame = input.Frame;

            // var status = localConnectStatus[queue];
            // status.LastFrame = input.Frame;
            // Send the input to all the remote players.
            for (var i = 0; i < numPlayers; i++)
                if (endpoints[i].IsInitialized())
                    endpoints[i].SendInput(input);
        }

        return ErrorCode.Ok;
    }

    public ErrorCode SynchronizeInputs(params TInput[] inputs) => SynchronizeInputs(out _, inputs);

    public ErrorCode SynchronizeInputs(out int[] disconnectFlags, params TInput[] inputs)
    {
        if (synchronizing)
        {
            disconnectFlags = Array.Empty<int>();
            return ErrorCode.NotSynchronized;
        }

        disconnectFlags = sync.SynchronizeInputs(inputs);
        return ErrorCode.Ok;
    }

    ErrorCode PlayerHandleToQueue(PlayerHandle player, out int queue)
    {
        var offset = player.Value - 1;
        if (offset < 0 || offset >= numPlayers)
        {
            queue = PlayerHandle.Empty.Value;
            return ErrorCode.InvalidPlayerHandle;
        }

        queue = offset;
        return ErrorCode.Ok;
    }

    protected PlayerHandle QueueToPlayerHandle(int queue) => new(queue + 1);

    protected PlayerHandle QueueToSpectatorHandle(int queue) =>
        new(queue + SpectatorOffset); /* out of range of the player array, basically */

    UdpProtocol CreateUdpProtocol(IPEndPoint endpoint, int queue) =>
        new(
            timesync: new(),
            udp: udp,
            queue, endpoint, localConnectStatus
        )
        {
            DisconnectTimeout = disconnectTimeout,
            DisconnectNotifyStart = disconnectNotifyStart,
        };

    void AddRemotePlayer(IPEndPoint endpoint, int queue)
    {
        /*
         * Start the state machine (xxx: no)
         */
        synchronizing = true;

        var protocol = CreateUdpProtocol(endpoint, queue);
        poll.RegisterLoop(protocol);
        protocol.Synchronize();
        endpoints.Add(protocol);
    }

    ErrorCode AddSpectator(IPEndPoint endpoint)
    {
        if (numSpectators == Max.Spectators)
            return ErrorCode.TooManySpectators;

        /*
         * Currently, we can only add spectators before the game starts.
         */
        if (!synchronizing)
            return ErrorCode.InvalidRequest;

        var queue = numSpectators++;
        var protocol = CreateUdpProtocol(endpoint, queue + SpectatorOffset);

        poll.RegisterLoop(protocol);
        protocol.Synchronize();
        spectators.Add(protocol);

        return ErrorCode.Ok;
    }

    public void OnMsg(IPEndPoint from, UdpMsg msg, int len)
    {
        for (var i = 0; i < numPlayers; i++)
        {
            if (!endpoints[i].HandlesMsg(from, msg)) continue;
            endpoints[i].OnMsg(msg, len);
            return;
        }

        for (int i = 0; i < numSpectators; i++)
        {
            if (!spectators[i].HandlesMsg(from, msg)) continue;
            spectators[i].OnMsg(msg, len);
            return;
        }
    }

    public void Dispose()
    {
        udp.OnMsg -= OnMsg;
        udp.Dispose();
    }
}