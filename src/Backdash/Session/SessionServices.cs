using Backdash.Core;
using Backdash.Network;
using Backdash.Network.Client;
using Backdash.Network.Protocol;
using Backdash.Options;
using Backdash.Serialization;
using Backdash.Synchronizing.Input.Confirmed;
using Backdash.Synchronizing.Random;
using Backdash.Synchronizing.State;

namespace Backdash;

sealed class SessionServices<TInput> where TInput : unmanaged
{
    public IBinarySerializer<TInput> InputSerializer { get; }
    public IChecksumProvider ChecksumProvider { get; }
    public Logger Logger { get; }
    public BackgroundJobManager JobManager { get; }
    public IProtocolClientFactory ProtocolClientFactory { get; }
    public IStateStore StateStore { get; }
    public IRandomNumberGenerator Random { get; }
    public IDeterministicRandom<TInput> DeterministicRandom { get; }
    public IDelayStrategy DelayStrategy { get; }
    public IInputListener<TInput>? InputListener { get; }

    public EqualityComparer<TInput> InputComparer { get; }

    public INetcodeSessionHandler SessionHandler { get; }

    public PluginManager PluginManager { get; }

    public SessionServices(
        IBinarySerializer<TInput> inputSerializer,
        NetcodeOptions options,
        ServicesConfig<TInput>? services
    )
    {
        ArgumentNullException.ThrowIfNull(inputSerializer);
        ArgumentNullException.ThrowIfNull(options);

        ChecksumProvider = services?.ChecksumProvider ?? new Fletcher32ChecksumProvider();
        StateStore = services?.StateStore ?? new DefaultStateStore(options.StateSizeHint);
        DeterministicRandom = services?.DeterministicRandom ?? new XorShiftRandom<TInput>();
        InputListener = services?.InputListener;
        Random = new DefaultRandomNumberGenerator(services?.Random ?? System.Random.Shared);
        DelayStrategy = DelayStrategyFactory.Create(Random, options.Protocol.DelayStrategy);
        InputComparer = services?.InputComparer ?? EqualityComparer<TInput>.Default;
        InputSerializer = inputSerializer;

        var logWriter = services?.LogWriter ?? new ConsoleTextLogWriter();
        Logger = new(options.Logger, logWriter);
        JobManager = new(Logger);
        SessionHandler = services?.SessionHandler ?? new EmptySessionHandler(Logger);

        var socketFactory = services?.PeerSocketFactory ?? new PeerSocketFactory();
        ProtocolClientFactory = new ProtocolClientFactory(options, socketFactory, Logger, DelayStrategy);
        PluginManager = new(Logger, services?.Plugins ?? []);
    }
}
