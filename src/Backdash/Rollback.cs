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
        ILogWriter? logWriter = null
    )
        where TInput : struct
        where TGameState : struct, IEquatable<TGameState>
    {
        inputSerializer ??= BinarySerializerFactory.FindOrThrow<TInput>(options.EnableEndianness);
        var factory = new UdpClientFactory();
        var logger = new Logger(options.LogLevel, logWriter ?? new ConsoleLogWriter());
        var checksumProvider = new HashCodeChecksumProvider<TGameState>();
        var stateStore = StateStoreFactory.Create(stateSerializer);

        return new Peer2PeerBackend<TInput, TGameState>(
            options,
            inputSerializer,
            stateStore,
            checksumProvider,
            factory,
            new BackgroundJobManager(logger),
            new ProtocolEventQueue(),
            logger
        );
    }

    public static IRollbackSession<TInput, TGameState> CreateTestSession<TInput, TGameState>(
        RollbackOptions options,
        IRollbackHandler<TGameState>? callbacks = null,
        IBinarySerializer<TInput>? inputSerializer = null,
        IBinarySerializer<TGameState>? stateSerializer = null,
        ILogWriter? logWriter = null
    )
        where TInput : struct
        where TGameState : struct, IEquatable<TGameState>
    {
        inputSerializer ??= BinarySerializerFactory.FindOrThrow<TInput>(options.EnableEndianness);
        var logger = new Logger(options.LogLevel, logWriter ?? new ConsoleLogWriter());
        var stateStore = StateStoreFactory.Create(stateSerializer);
        var checksumProvider = new HashCodeChecksumProvider<TGameState>();

        callbacks ??= new EmptySessionHandler<TGameState>(logger);

        return new SyncTestBackend<TInput, TGameState>(
            callbacks,
            options,
            inputSerializer,
            stateStore,
            checksumProvider,
            logger
        );
    }
}
