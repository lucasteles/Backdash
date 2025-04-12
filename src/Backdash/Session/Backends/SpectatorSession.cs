using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Backdash.Core;
using Backdash.Network;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol;
using Backdash.Options;
using Backdash.Serialization;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.Input.Confirmed;
using Backdash.Synchronizing.Random;
using Backdash.Synchronizing.State;

namespace Backdash.Backends;

sealed class SpectatorSession<TInput> :
    INetcodeSession<TInput>,
    IProtocolNetworkEventHandler,
    IProtocolInputEventPublisher<ConfirmedInputs<TInput>>
    where TInput : unmanaged
{
    readonly Logger logger;
    readonly IProtocolPeerClient udp;
    readonly IPEndPoint hostEndpoint;
    readonly NetcodeOptions options;
    readonly IBackgroundJobManager backgroundJobManager;
    readonly ConnectionsState localConnections = new(0);
    readonly GameInput<ConfirmedInputs<TInput>>[] inputs;
    readonly PeerConnection<ConfirmedInputs<TInput>> host;
    readonly FrozenSet<PlayerHandle> fakePlayers;
    readonly IDeterministicRandom<TInput> random;

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
    readonly Endianness endianness;

    public SpectatorSession(
        SpectatorOptions spectatorOptions,
        NetcodeOptions options,
        SessionServices<TInput> services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(spectatorOptions);
        ArgumentNullException.ThrowIfNull(spectatorOptions.HostAddress);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.LocalPort);

        this.options = options;
        hostEndpoint = spectatorOptions.HostEndPoint;
        backgroundJobManager = services.JobManager;
        random = services.DeterministicRandom;
        logger = services.Logger;
        stateStore = services.StateStore;
        checksumProvider = services.ChecksumProvider;
        NumberOfPlayers = options.NumberOfPlayers;
        fakePlayers = Enumerable.Range(0, options.NumberOfPlayers)
            .Select(x => new PlayerHandle(PlayerType.Remote, x)).ToFrozenSet();
        IBinarySerializer<ConfirmedInputs<TInput>> inputGroupSerializer =
            new ConfirmedInputsSerializer<TInput>(services.InputSerializer);
        PeerObserverGroup<ProtocolMessage> peerObservers = new();
        inputs = new GameInput<ConfirmedInputs<TInput>>[options.SpectatorInputBufferLength];
        callbacks = services.SessionHandler;
        endianness = options.GetStateSerializationEndianness();
        udp = services.ProtocolClientFactory.CreateProtocolClient(options.LocalPort, peerObservers);
        backgroundJobManager.Register(udp);
        var magicNumber = services.Random.MagicNumber();


        PeerConnectionFactory peerConnectionFactory = new(
            this, services.Random, logger, udp,
            options.Protocol, options.TimeSync, stateStore
        );

        ProtocolState protocolState =
            new(new(PlayerType.Remote, 0), hostEndpoint, localConnections, magicNumber);

        var inputGroupComparer = ConfirmedInputComparer<TInput>.Create(services.InputComparer);
        host = peerConnectionFactory.Create(protocolState, inputGroupSerializer, this, inputGroupComparer);
        stateStore.Initialize(options.TotalSavedFramesAllowed);
        peerObservers.Add(host.GetUdpObserver());
        isSynchronizing = true;
        host.Synchronize();
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
    public SavedFrame GetCurrentSavedFrame() => stateStore.Last();
    public INetcodeRandom Random => random;
    public int NumberOfPlayers { get; private set; }
    public int NumberOfSpectators => 0;
    public int LocalPort => udp.BindPort;

    public ReadOnlySpan<SynchronizedInput<TInput>> CurrentSynchronizedInputs => syncInputBuffer;

    public ReadOnlySpan<TInput> CurrentInputs => inputBuffer;

    public SessionMode Mode => SessionMode.Spectator;

    public void DisconnectPlayer(in PlayerHandle player) { }
    public ResultCode AddLocalInput(in PlayerHandle player, in TInput localInput) => ResultCode.Ok;
    public IReadOnlySet<PlayerHandle> GetPlayers() => fakePlayers;
    public IReadOnlySet<PlayerHandle> GetSpectators() => FrozenSet<PlayerHandle>.Empty;

    public ResultCode AddLocalPlayer(out PlayerHandle handle)
    {
        handle = default;
        return ResultCode.NotSupported;
    }

    public ResultCode AddRemotePlayer(IPEndPoint endpoint, out PlayerHandle handle)
    {
        handle = default;
        return ResultCode.NotSupported;
    }

#pragma warning disable S4144
    public ResultCode AddSpectator(IPEndPoint endpoint, out PlayerHandle handle)
    {
        handle = default;
        return ResultCode.NotSupported;
    }
#pragma warning restore S4144

    public void BeginFrame()
    {
        host.Update();
        backgroundJobManager.ThrowIfError();

        if (isSynchronizing)
            return;

        if (lastReceivedInputTime > 0 &&
            Stopwatch.GetElapsedTime(lastReceivedInputTime) > options.Protocol.DisconnectTimeout)
            Close();

        if (CurrentFrame.Number == 0)
            SaveCurrentFrame();
    }

    public void AdvanceFrame()
    {
        logger.Write(LogLevel.Debug, $"[End Frame {CurrentFrame}]");

        CurrentFrame++;
        SaveCurrentFrame();
    }

    public PlayerConnectionStatus GetPlayerStatus(in PlayerHandle player) => host.Status.ToPlayerStatus();

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

    [MemberNotNull(nameof(callbacks))]
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
                host.Start();
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

        random.UpdateSeed(CurrentFrame, inputBuffer, extraSeedState);
        return ResultCode.Ok;
    }

    uint extraSeedState;
    public void SetRandomSeed(uint seed, uint extraState = 0) => extraSeedState = unchecked(seed + extraState);

    void SaveCurrentFrame()
    {
        ref var nextState = ref stateStore.Next();

        BinaryBufferWriter writer = new(nextState.GameState, endianness);
        callbacks.SaveState(CurrentFrame, in writer);
        nextState.Frame = CurrentFrame;
        nextState.Checksum = checksumProvider.Compute(nextState.GameState.WrittenSpan);

        stateStore.Advance();
        logger.Write(LogLevel.Trace, $"spectator: saved frame {nextState.Frame} (checksum: {nextState.Checksum:x8}).");
    }

    public bool LoadFrame(Frame frame)
    {
        frame = Frame.Max(in frame, in Frame.Zero);

        if (frame.Number == CurrentFrame.Number)
        {
            logger.Write(LogLevel.Trace, "Skipping NOP.");
            return true;
        }

        if (!stateStore.TryLoad(in frame, out var savedFrame))
            return false;

        logger.Write(LogLevel.Trace,
            $"Loading replay frame {savedFrame.Frame} (checksum: {savedFrame.Checksum:x8})");

        var offset = 0;
        BinaryBufferReader reader = new(savedFrame.GameState.WrittenSpan, ref offset, endianness);
        callbacks.LoadState(in frame, in reader);
        CurrentFrame = savedFrame.Frame;
        return true;
    }

    bool IProtocolInputEventPublisher<ConfirmedInputs<TInput>>.Publish(in GameInputEvent<ConfirmedInputs<TInput>> evt)
    {
        lastReceivedInputTime = Stopwatch.GetTimestamp();
        var (_, input) = evt;
        inputs[input.Frame.Number % inputs.Length] = input;
        host.SetLocalFrameNumber(input.Frame, options.FrameRate);
        return host.SendInputAck();
    }
}
