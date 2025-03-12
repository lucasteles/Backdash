using Backdash.Core;
using Backdash.Network;
using Backdash.Network.Client;
using Backdash.Network.Protocol;
using Backdash.Serialization;
using Backdash.Serialization.Internal;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.Input.Confirmed;
using Backdash.Synchronizing.Random;
using Backdash.Synchronizing.State;

namespace Backdash.Backends;

sealed class BackendServices<TInput> where TInput : unmanaged
{
    public IBinarySerializer<TInput> InputSerializer { get; }
    public IChecksumProvider ChecksumProvider { get; }
    public Logger Logger { get; }
    public IBackgroundJobManager JobManager { get; }
    public IProtocolClientFactory ProtocolClientFactory { get; }
    public IStateStore StateStore { get; }
    public IInputGenerator<TInput>? InputGenerator { get; }
    public IRandomNumberGenerator Random { get; }
    public IDeterministicRandom<TInput> DeterministicRandom { get; }
    public IDelayStrategy DelayStrategy { get; }
    public IInputListener<TInput>? InputListener { get; }

    public EqualityComparer<TInput> InputComparer { get; }

    public BackendServices(NetcodeOptions options, SessionServices<TInput>? services)
    {
        ChecksumProvider = services?.ChecksumProvider ?? new Fletcher32ChecksumProvider();
        StateStore = services?.StateStore ?? new DefaultStateStore(options.StateSizeHint);
        DeterministicRandom = services?.DeterministicRandom ?? new XorShiftRandom<TInput>();
        InputListener = services?.InputListener;
        Random = new DefaultRandomNumberGenerator(services?.Random ?? System.Random.Shared);
        DelayStrategy = DelayStrategyFactory.Create(Random, options.Protocol.DelayStrategy);
        InputGenerator = services?.InputGenerator;
        InputComparer = services?.InputComparer ?? EqualityComparer<TInput>.Default;

        InputSerializer = services?.InputSerializer ?? BinarySerializerFactory
            .FindOrThrow<TInput>(options.Protocol.SerializationEndianness);

        var logWriter = services?.LogWriter is null || options.Logger.EnabledLevel is LogLevel.None
            ? new ConsoleTextLogWriter()
            : services.LogWriter;

        Logger = new(options.Logger, logWriter);
        JobManager = new BackgroundJobManager(Logger);

        var socketFactory = services?.PeerSocketFactory ?? new PeerSocketFactory();
        ProtocolClientFactory = new ProtocolClientFactory(options, socketFactory, Logger, DelayStrategy);
    }
}

static class BackendServices
{
    public static BackendServices<TInput> Create<TInput>(NetcodeOptions options, SessionServices<TInput>? services)
        where TInput : unmanaged =>
        new(options, services);
}
