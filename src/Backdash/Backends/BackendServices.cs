using Backdash.Core;
using Backdash.Network;
using Backdash.Network.Protocol;
using Backdash.Serialization;
using Backdash.Sync.Input;
using Backdash.Sync.State;
using Backdash.Sync.State.Stores;
namespace Backdash.Backends;
sealed class BackendServices<TInput, TGameState>
    where TInput : struct
    where TGameState : IEquatable<TGameState>, new()
{
    public IBinarySerializer<TInput> InputSerializer { get; }
    public IChecksumProvider<TGameState> ChecksumProvider { get; }
    public Logger Logger { get; }
    public IClock Clock { get; }
    public IBackgroundJobManager JobManager { get; }
    public IUdpClientFactory UdpClientFactory { get; }
    public IStateStore<TGameState> StateStore { get; }
    public IInputGenerator<TInput>? InputGenerator { get; }
    public IRandomNumberGenerator Random { get; }
    public IDelayStrategy DelayStrategy { get; }
    public BackendServices(RollbackOptions options, SessionServices<TInput, TGameState>? services)
    {
        ChecksumProvider = services?.ChecksumProvider ?? ChecksumProviderFactory.Create<TGameState>();
        StateStore = services?.StateStore ?? StateStoreFactory.Create(services?.StateSerializer);
        Random = new DefaultRandomNumberGenerator(services?.Random ?? System.Random.Shared);
        DelayStrategy = DelayStrategyFactory.Create(Random, options.Protocol.DelayStrategy);
        InputGenerator = services?.InputGenerator;
        InputSerializer = services?.InputSerializer ?? BinarySerializerFactory
            .FindOrThrow<TInput>(options.NetworkEndianness);
        var logWriter = services?.LogWriter is null || options.Log.EnabledLevel is LogLevel.None
            ? new ConsoleTextLogWriter()
            : services.LogWriter;
        Logger = new Logger(options.Log, logWriter);
        Clock = new Clock();
        JobManager = new BackgroundJobManager(Logger);
        UdpClientFactory = new UdpClientFactory();
    }
}
static class BackendServices
{
    public static BackendServices<TInput, TGameState> Create<TInput, TGameState>(
        RollbackOptions options,
        SessionServices<TInput, TGameState>? services
    )
        where TGameState : IEquatable<TGameState>, new()
        where TInput : struct =>
        new(options, services);
}
