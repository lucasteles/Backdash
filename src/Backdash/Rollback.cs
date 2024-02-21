using Backdash.Backends;
using Backdash.Core;
using Backdash.Network;
using Backdash.Network.Protocol;
using Backdash.Serialization;
using Backdash.Sync.State;
using Backdash.Sync.State.Stores;

namespace Backdash;

public static class Rollback
{
    public static IRollbackSession<TInput, TGameState> CreateSession<TInput, TGameState>(
        RollbackOptions options,
        IBinarySerializer<TInput>? inputSerializer = null,
        IBinarySerializer<TGameState>? stateSerializer = null,
        IChecksumProvider<TGameState>? checksumProvider = null,
        ILogWriter? logWriter = null
    )
        where TInput : struct
        where TGameState : IEquatable<TGameState>, new()
    {
        inputSerializer ??= BinarySerializerFactory.FindOrThrow<TInput>(options.NetworkEndianness);
        checksumProvider ??= ChecksumProviderFactory.Create<TGameState>();
        var factory = new UdpClientFactory();
        var logger = new Logger(options.Log, logWriter ?? new ConsoleLogWriter());
        var stateStore = StateStoreFactory.Create(stateSerializer);
        var clock = new Clock();

        return new Peer2PeerBackend<TInput, TGameState>(
            options,
            inputSerializer,
            stateStore,
            checksumProvider,
            factory,
            new BackgroundJobManager(logger),
            new ProtocolEventQueue<TInput>(),
            clock,
            logger
        );
    }

    public static IRollbackSession<TInput, TGameState> CreateTestSession<TInput, TGameState>(
        RollbackOptions options,
        IChecksumProvider<TGameState>? checksumProvider = null,
        IBinarySerializer<TGameState>? stateSerializer = null,
        ILogWriter? logWriter = null
    )
        where TInput : struct
        where TGameState : IEquatable<TGameState>, new()
    {
        checksumProvider ??= ChecksumProviderFactory.Create<TGameState>();

        var logger = new Logger(options.Log, logWriter ?? new ConsoleLogWriter());
        var stateStore = StateStoreFactory.Create(stateSerializer);

        return new SyncTestBackend<TInput, TGameState>(
            options,
            stateStore,
            checksumProvider,
            logger
        );
    }
}
