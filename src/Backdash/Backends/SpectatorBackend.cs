using System.Net;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol;
using Backdash.Serialization;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.Input.Confirmed;
using Backdash.Synchronizing.Random;
using Backdash.Synchronizing.State;

namespace Backdash.Backends;

sealed class SpectatorBackend<TInput> :
    INetcodeSession<TInput>,
    IProtocolNetworkEventHandler,
    IProtocolInputEventPublisher<ConfirmedInputs<TInput>>
    where TInput : unmanaged
{
    readonly Logger logger;
    readonly IProtocolClient udp;
    readonly IPEndPoint hostEndpoint;
    readonly NetcodeOptions options;
    readonly IBackgroundJobManager backgroundJobManager;
    readonly IClock clock;
    readonly ConnectionsState localConnections = new(0);
    readonly GameInput<ConfirmedInputs<TInput>>[] inputs;
    readonly PeerConnection<ConfirmedInputs<TInput>> host;
    readonly PlayerHandle[] fakePlayers;

    INetcodeSessionHandler callbacks;
    bool isSynchronizing;
    Task backgroundJobTask = Task.CompletedTask;
    bool disposed;
    long lastReceivedInputTime;
    SynchronizedInput<TInput>[] syncInputBuffer = [];
    TInput[] inputBuffer = [];
    bool closed;
    readonly IStateStore stateStore;
    readonly IChecksumProvider checksumProvider;

    public SpectatorBackend(int port,
        IPEndPoint hostEndpoint,
        int numberOfPlayers,
        NetcodeOptions options,
        BackendServices<TInput> services)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(hostEndpoint);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);

        this.hostEndpoint = hostEndpoint;
        this.options = options;
        backgroundJobManager = services.JobManager;
        Random = services.DeterministicRandom;
        logger = services.Logger;
        clock = services.Clock;
        stateStore = services.StateStore;
        checksumProvider = services.ChecksumProvider;
        NumberOfPlayers = numberOfPlayers;
        fakePlayers = Enumerable.Range(0, numberOfPlayers)
            .Select(x => new PlayerHandle(PlayerType.Remote, x + 1, x)).ToArray();
        IBinarySerializer<ConfirmedInputs<TInput>> inputGroupSerializer =
            new ConfirmedInputsSerializer<TInput>(services.InputSerializer);
        PeerObserverGroup<ProtocolMessage> peerObservers = new();
        callbacks = new EmptySessionHandler(logger);
        inputs = new GameInput<ConfirmedInputs<TInput>>[options.SpectatorInputBufferLength];

        udp = services.ProtocolClientFactory.CreateProtocolClient(port, peerObservers);
        backgroundJobManager.Register(udp);
        var magicNumber = services.Random.MagicNumber();

        PeerConnectionFactory peerConnectionFactory = new(
            this, clock, services.Random, logger, udp,
            options.Protocol, options.TimeSync, stateStore
        );

        ProtocolState protocolState =
            new(new(PlayerType.Remote, 0), hostEndpoint, localConnections, magicNumber);

        var inputGroupComparer = ConfirmedInputComparer<TInput>.Create(services.InputComparer);
        host = peerConnectionFactory.Create(protocolState, inputGroupSerializer, this, inputGroupComparer);

        peerObservers.Add(host.GetUdpObserver());
        host.Synchronize();
        isSynchronizing = true;
        stateStore.Initialize(options.TotalPredictionFrames);
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
        if (closed) return;
        closed = true;
        logger.Write(LogLevel.Information, "Shutting down connections");
        host.Dispose();
        callbacks.OnSessionClose();
    }

    public Frame CurrentFrame { get; private set; } = Frame.Zero;
    public FrameSpan RollbackFrames => FrameSpan.Zero;
    public FrameSpan FramesBehind => FrameSpan.Zero;
    public SavedFrame CurrentSavedFrame => stateStore.GetCurrent();

    public int NumberOfPlayers { get; private set; }
    public int NumberOfSpectators => 0;

    public IDeterministicRandom Random { get; }
    public SessionMode Mode => SessionMode.Spectating;

    public void DisconnectPlayer(in PlayerHandle player) { }
    public ResultCode AddLocalInput(PlayerHandle player, in TInput localInput) => ResultCode.Ok;
    public IReadOnlyCollection<PlayerHandle> GetPlayers() => fakePlayers;
    public IReadOnlyCollection<PlayerHandle> GetSpectators() => [];

    public void BeginFrame()
    {
        host.Update();
        backgroundJobManager.ThrowIfError();

        if (isSynchronizing)
            return;

        if (lastReceivedInputTime > 0 &&
            clock.GetElapsedTime(lastReceivedInputTime) > options.Protocol.DisconnectTimeout)
            Close();
    }

    public void AdvanceFrame() => logger.Write(LogLevel.Debug, $"[End Frame {CurrentFrame}]");

    public PlayerConnectionStatus GetPlayerStatus(in PlayerHandle player) => host.Status.ToPlayerStatus();
    public ResultCode AddPlayer(Player player) => ResultCode.NotSupported;

    public IReadOnlyList<ResultCode> AddPlayers(IReadOnlyList<Player> players) =>
        Enumerable.Repeat(ResultCode.NotSupported, players.Count).ToArray();

    public bool GetNetworkStatus(in PlayerHandle player, ref PeerNetworkStats info)
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

    public void SetHandler(INetcodeSessionHandler handler)
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
                isSynchronizing = false;
                break;
            case ProtocolEvent.SyncFailure:
                callbacks.OnPeerEvent(player, new(PeerEvent.SynchronizationFailure));
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

        ThrowIf.Assert(input.Data.Count > 0);
        NumberOfPlayers = input.Data.Count;

        if (syncInputBuffer.Length != NumberOfPlayers)
        {
            Array.Resize(ref syncInputBuffer, NumberOfPlayers);
            Array.Resize(ref inputBuffer, syncInputBuffer.Length);
        }

        for (var i = 0; i < NumberOfPlayers; i++)
        {
            syncInputBuffer[i] = new(input.Data.Inputs[i], false);
            inputBuffer[i] = input.Data.Inputs[i];
        }

        var inputPopCount = options.UseInputSeedForRandom ? Mem.PopCount<TInput>(inputBuffer.AsSpan()) : 0;
        Random.UpdateSeed(CurrentFrame.Number, inputPopCount);

        CurrentFrame++;
        SaveCurrentFrame();
        return ResultCode.Ok;
    }

    void SaveCurrentFrame()
    {
        var currentFrame = CurrentFrame;
        ref var nextState = ref stateStore.GetCurrent();

        BinaryBufferWriter writer = new(nextState.GameState);
        callbacks.SaveState(in currentFrame, in writer);
        nextState.Frame = currentFrame;
        nextState.Checksum = checksumProvider.Compute(nextState.GameState.WrittenSpan);

        stateStore.Advance();
        logger.Write(LogLevel.Trace, $"spectator: saved frame {nextState.Frame} (checksum: {nextState.Checksum}).");
    }

    public ref readonly SynchronizedInput<TInput> GetInput(int index) =>
        ref syncInputBuffer[index];

    public ref readonly SynchronizedInput<TInput> GetInput(in PlayerHandle player) =>
        ref syncInputBuffer[player.Number - 1];

    public void GetInputs(Span<SynchronizedInput<TInput>> buffer) => syncInputBuffer.CopyTo(buffer);

    bool IProtocolInputEventPublisher<ConfirmedInputs<TInput>>.Publish(in GameInputEvent<ConfirmedInputs<TInput>> evt)
    {
        lastReceivedInputTime = clock.GetTimeStamp();
        var (_, input) = evt;
        inputs[input.Frame.Number % inputs.Length] = input;
        host.SetLocalFrameNumber(input.Frame, options.FramesPerSecond);
        return host.SendInputAck();
    }
}
