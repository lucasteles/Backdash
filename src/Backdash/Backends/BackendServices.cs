using Backdash.Core;
using Backdash.Network;
using Backdash.Network.Client;
using Backdash.Network.Protocol;
using Backdash.Serialization;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.Input.Confirmed;
using Backdash.Synchronizing.State;
using Backdash.Synchronizing.State.Stores;

namespace Backdash.Backends;

sealed class BackendServices<TInput, TGameState>
    where TInput : unmanaged
    where TGameState : notnull, new()
{
    public IBinarySerializer<TInput> InputSerializer { get; }
    public IChecksumProvider<TGameState> ChecksumProvider { get; }
    public Logger Logger { get; }
    public IClock Clock { get; }
    public IBackgroundJobManager JobManager { get; }
    public IProtocolClientFactory ProtocolClientFactory { get; }
    public IStateStore<TGameState> StateStore { get; }
    public IInputGenerator<TInput>? InputGenerator { get; }
    public IRandomNumberGenerator Random { get; }
    public IDelayStrategy DelayStrategy { get; }
    public IInputListener<TInput>? InputListener { get; }

    public BackendServices(RollbackOptions options, SessionServices<TInput, TGameState>? services)
    {
        ChecksumProvider = services?.ChecksumProvider ?? ChecksumProviderFactory.Create<TGameState>();
        StateStore = services?.StateStore ?? StateStoreFactory.Create(services?.StateSerializer);
        InputListener = services?.InputListener;
        Random = new DefaultRandomNumberGenerator(services?.Random ?? System.Random.Shared);
        DelayStrategy = DelayStrategyFactory.Create(Random, options.Protocol.DelayStrategy);
        InputGenerator = services?.InputGenerator;

        InputSerializer = services?.InputSerializer ?? BinarySerializerFactory
            .FindOrThrow<TInput>(options.NetworkEndianness);

        var logWriter = services?.LogWriter is null || options.Log.EnabledLevel is LogLevel.None
            ? new ConsoleTextLogWriter()
            : services.LogWriter;

        Logger = new(options.Log, logWriter);
        Clock = new Clock();
        JobManager = new BackgroundJobManager(Logger);

        var socketFactory = services?.PeerSocketFactory ?? new PeerSocketFactory();
        ProtocolClientFactory = new ProtocolClientFactory(options, socketFactory, Logger);
    }
}

static class BackendServices
{
    public static BackendServices<TInput, TGameState> Create<TInput, TGameState>(
        RollbackOptions options,
        SessionServices<TInput, TGameState>? services
    )
        where TGameState : notnull, new()
        where TInput : unmanaged =>
        new(options, services);
}
