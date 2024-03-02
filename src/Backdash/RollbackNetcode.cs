using System.Net;
using Backdash.Backends;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Network.Protocol;
using Backdash.Serialization;
using Backdash.Sync.State;
using Backdash.Sync.State.Stores;

namespace Backdash;

public static class RollbackNetcode
{
    public static IRollbackSession<TInput, TGameState> CreateSession<TInput, TGameState>(
        int port,
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
        options.LocalPort = port;
        var factory = new UdpClientFactory();
        var stateStore = StateStoreFactory.Create(stateSerializer);
        var clock = new Clock();
        var logger = new Logger(options.Log,
            logWriter is null || options.Log.EnabledLevel is LogLevel.Off
                ? new ConsoleLogWriter()
                : logWriter);

        return new Peer2PeerBackend<TInput, TGameState>(
            options,
            inputSerializer,
            stateStore,
            checksumProvider,
            factory,
            new BackgroundJobManager(logger),
            new ProtocolInputEventQueue<TInput>(),
            clock,
            logger
        );
    }

    public static IRollbackSession<TInput, TGameState> CreateSpectatorSession<TInput, TGameState>(
        int port,
        RollbackOptions options,
        IPEndPoint host,
        IBinarySerializer<TInput>? inputSerializer = null,
        ILogWriter? logWriter = null
    )
        where TInput : struct
        where TGameState : IEquatable<TGameState>, new()
    {
        options.LocalPort = port;
        inputSerializer ??= BinarySerializerFactory.FindOrThrow<TInput>(options.NetworkEndianness);
        var factory = new UdpClientFactory();
        var clock = new Clock();

        var logger = new Logger(options.Log,
            logWriter is null || options.Log.EnabledLevel is LogLevel.Off
                ? new ConsoleLogWriter()
                : logWriter
        );

        return new SpectatorBackend<TInput, TGameState>(
            options,
            host,
            inputSerializer,
            factory,
            new BackgroundJobManager(logger),
            clock,
            logger
        );
    }

    public static IRollbackSession<TInput, TGameState> CreateTestSession<TInput, TGameState>(
        RollbackOptions? options = null,
        FrameSpan? checkDistance = null,
        IChecksumProvider<TGameState>? checksumProvider = null,
        IBinarySerializer<TGameState>? stateSerializer = null,
        ILogWriter? logWriter = null
    )
        where TInput : struct
        where TGameState : IEquatable<TGameState>, new()
    {
        checksumProvider ??= ChecksumProviderFactory.Create<TGameState>();
        options ??= new()
        {
            Log = new(LogLevel.Debug),
        };

        checkDistance ??= FrameSpan.One;
        var stateStore = StateStoreFactory.Create(stateSerializer);
        var clock = new Clock();
        var logger = new Logger(options.Log,
            logWriter is null || options.Log.EnabledLevel is LogLevel.Off
                ? new ConsoleLogWriter()
                : logWriter
        );


        return new SyncTestBackend<TInput, TGameState>(
            options,
            checkDistance.Value,
            stateStore,
            checksumProvider,
            clock,
            logger
        );
    }
}
