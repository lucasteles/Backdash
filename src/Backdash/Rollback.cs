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
        ILogWriter? logWriter = null
    )
        where TInput : struct
        where TGameState : struct
    {
        inputSerializer ??= BinarySerializerFactory.FindOrThrow<TInput>(options.EnableEndianness);
        UdpClientFactory factory = new();
        Logger logger = new(options.LogLevel, logWriter ?? new ConsoleLogWriter());
        ArrayStateStore<TGameState> stateStore = new(options);
        DefaultChecksumProvider<TGameState> checksumProvider = new();

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
        ILogWriter? logWriter = null
    )
        where TInput : struct
        where TGameState : struct
    {
        inputSerializer ??= BinarySerializerFactory.FindOrThrow<TInput>(options.EnableEndianness);
        Logger logger = new(options.LogLevel, logWriter ?? new ConsoleLogWriter());
        callbacks ??= new EmptySessionHandler<TGameState>(logger);
        ArrayStateStore<TGameState> stateStore = new(options);
        DefaultChecksumProvider<TGameState> checksumProvider = new();

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
