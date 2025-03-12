using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Backdash.Backends;
using Backdash.Core;
using Backdash.Network;
using Backdash.Options;
using Backdash.Serialization;
using Backdash.Serialization.Internal;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.Input.Confirmed;

// ReSharper disable LocalVariableHidesMember, ParameterHidesMember
#pragma warning disable S2325, CA1822

namespace Backdash;

/// <inheritdoc cref="NetcodeSessionBuilder"/>
[SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
public sealed class NetcodeSessionBuilder<TInput> where TInput : unmanaged
{
    readonly NetcodeSessionBuilder.SerializerFactory<TInput> serializer;
    NetcodeOptions options = new();
    SessionServices<TInput>? sessionServices;
    SessionMode sessionMode = SessionMode.Remote;

    SyncTestOptions<TInput>? syncTestOptions;
    SessionReplayOptions<TInput>? replayOptions;
    SpectatorOptions? spectatorOptions;

    INetcodeSessionHandler? sessionHandler;
    readonly List<Player> playerList = [];

    internal NetcodeSessionBuilder(NetcodeSessionBuilder.SerializerFactory<TInput> serializer) =>
        this.serializer = serializer;

    /// <summary>
    /// Builds a new <see cref="INetcodeSession{TInput}"/>.
    /// </summary>
    public INetcodeSession<TInput> Build()
    {
        this.options.EnsureDefaults();
        var options = this.options.CloneOptions();

        BackendServices<TInput> services = new(
            serializer.Invoke(options.Protocol.SerializationEndianness),
            options, sessionServices);

        var session = GetSession();

        foreach (var player in playerList)
        {
            var addResult = session.AddPlayer(player);
            if (addResult is not ResultCode.Ok)
                throw new InvalidOperationException($"Failed to add player {playerList}: {addResult}");
        }

        if (sessionHandler is not null)
            session.SetHandler(sessionHandler);

        return session;

        INetcodeSession<TInput> GetSession()
        {
            switch (sessionMode)
            {
                case SessionMode.Remote:
                    return new RemoteBackend<TInput>(options, services);
                case SessionMode.Local:
                    ConfigureLocal();
                    return new LocalBackend<TInput>(options, services);
                case SessionMode.Spectator:
                    ConfigureSpectator();
                    return new SpectatorBackend<TInput>(spectatorOptions, options, services);
                case SessionMode.Replay:
                    ConfigureReplay();
                    return new ReplayBackend<TInput>(replayOptions, options, services);
                case SessionMode.SyncTest:
                    ConfigureSyncTest();
                    return new SyncTestBackend<TInput>(syncTestOptions, options, services);
                default:
                    throw new InvalidOperationException($"Unknown session mode: {sessionMode}");
            }
        }
    }

    void ConfigureLocal()
    {
        if (playerList.Count > 0) return;

        for (var i = 0; i < options.NumberOfPlayers; i++)
            playerList.Add(new LocalPlayer(i));
    }

    /// <summary>
    /// Set the <see cref="SessionMode"/> as <see cref="SessionMode.Remote"/>.
    /// </summary>
    public NetcodeSessionBuilder<TInput> ForRemote() => WithMode(SessionMode.Remote);

    /// <summary>
    /// Set the <see cref="SessionMode"/> as <see cref="SessionMode.Local"/>.
    /// </summary>
    public NetcodeSessionBuilder<TInput> ForLocal() => WithMode(SessionMode.Local);

    /// <summary>
    /// Set the <see cref="SessionMode"/> as <see cref="SessionMode.Spectator"/>.
    /// </summary>
    public NetcodeSessionBuilder<TInput> ForSpectator(Action<SpectatorOptions>? config = null) =>
        ConfigureSpectator(config);

    /// <summary>
    /// Set the <see cref="SessionMode"/> as <see cref="SessionMode.Replay"/>.
    /// </summary>
    public NetcodeSessionBuilder<TInput> ForReplay(Action<SessionReplayOptions<TInput>>? config = null) =>
        ConfigureReplay(config);

    /// <summary>
    /// Set the <see cref="SessionMode"/> as <see cref="SessionMode.SyncTest"/>.
    /// </summary>
    public NetcodeSessionBuilder<TInput> ForSyncTest(Action<SyncTestOptions<TInput>>? config = null) =>
        ConfigureSyncTest(config);

    /// <summary>
    /// Set the <see cref="SessionMode"/> for the <see cref="INetcodeSession{TInput}"/> to be build.
    /// </summary>
    /// <value>Defaults to <see cref="SessionMode.Remote"/></value>
    public NetcodeSessionBuilder<TInput> WithMode(SessionMode mode)
    {
        if (!Enum.IsDefined(sessionMode))
            throw new ArgumentOutOfRangeException(nameof(mode), mode, null);

        sessionMode = mode;
        return this;
    }

    /// <summary>
    /// Sets the number of players for the <see cref="INetcodeSession{TInput}"/>
    /// </summary>
    /// <value>Defaults to <c>2</c></value>
    public NetcodeSessionBuilder<TInput> WithPlayerCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, Max.NumberOfPlayers);
        options.NumberOfPlayers = count;
        return this;
    }

    /// <summary>
    /// Set the players for the <see cref="INetcodeSession{TInput}"/>
    /// </summary>
    public NetcodeSessionBuilder<TInput> WithPlayers(params Player[] players)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentOutOfRangeException.ThrowIfZero(players.Length);
        playerList.AddRange(players);
        return WithPlayerCount(Math.Max(players.Length, options.NumberOfPlayers));
    }

    /// <summary>
    /// Set the session handler for the <see cref="INetcodeSession{TInput}"/>
    /// </summary>
    /// <seealso cref="INetcodeSessionHandler"/>
    /// <seealso cref="INetcodeSession{TInput}.SetHandler"/>
    public NetcodeSessionBuilder<TInput> WithHandler(INetcodeSessionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        sessionHandler = handler;
        return this;
    }

    /// <inheritdoc cref="NetcodeOptions.InputDelayFrames"/>
    public NetcodeSessionBuilder<TInput> WithInputDelayFrames(int frames)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frames);
        options.InputDelayFrames = frames;

        return this;
    }

    /// <inheritdoc cref="NetcodeOptions.LocalPort"/>
    public NetcodeSessionBuilder<TInput> WithPort(int port)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        options.LocalPort = port;
        return this;
    }

    /// <summary>
    /// Select the input serialization <see cref="Endianness"/>
    /// </summary>
    /// <value>Defaults to <see cref="Endianness.BigEndian"/></value>
    /// <seealso cref="Platform"/>
    public NetcodeSessionBuilder<TInput> WithEndianness(Endianness endianness)
    {
        options.Protocol.SerializationEndianness = endianness;
        return this;
    }

    /// <summary>
    /// If <paramref name="useNetworkEndianness"/> is true,
    /// sets the input serialization <see cref="Endianness"/> to <see cref="Endianness.BigEndian"/>
    /// </summary>
    /// <seealso cref="Platform"/>
    public NetcodeSessionBuilder<TInput> WithNetworkEndianness(bool useNetworkEndianness = true) =>
        WithEndianness(Platform.GetNetworkEndianness(useNetworkEndianness));

    /// <summary>
    /// Configure <see cref="INetcodeSession{TInput}"/> options
    /// </summary>
    /// <seealso cref="NetcodeOptions"/>
    public NetcodeSessionBuilder<TInput> Configure(Action<NetcodeOptions> config)
    {
        config.Invoke(options);
        return this;
    }

    /// <summary>
    /// Set <see cref="INetcodeSession{TInput}"/> options
    /// </summary>
    /// <seealso cref="NetcodeOptions"/>
    public NetcodeSessionBuilder<TInput> WithLogLevel(LogLevel level, bool appendLevel = true)
    {
        options.Logger.EnabledLevel = level;
        options.Logger.AppendLevel = appendLevel;
        return this;
    }

    /// <summary>
    /// Set the logger <see cref="SessionServices{TInput}.LogWriter"/>
    /// </summary>
    /// <seealso cref="NetcodeOptions.Logger"/>
    [MemberNotNull(nameof(sessionServices))]
    public NetcodeSessionBuilder<TInput> WithLogWriter(ILogWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        return ConfigureServices(s => s.LogWriter = writer);
    }

    /// <summary>
    /// Set the logger <see cref="SessionServices{TInput}.LogWriter"/>
    /// </summary>
    /// <seealso cref="NetcodeOptions.Logger"/>
    [MemberNotNull(nameof(sessionServices))]
    public NetcodeSessionBuilder<TInput> WithLogWriter(Action<LogLevel, string> logAction) =>
        WithLogWriter(new DelegateLogWriter(logAction));

    /// <inheritdoc cref="WithLogWriter(ILogWriter)"/>
    /// <seealso cref="FileTextLogWriter"/>
    [MemberNotNull(nameof(sessionServices))]
    public NetcodeSessionBuilder<TInput> WithFileLogWriter(string? filename = null, bool append = true) =>
        WithLogWriter(new FileTextLogWriter(filename, append));

    /// <summary>
    /// Set the logger <see cref="SessionServices{TInput}.InputListener"/>
    /// </summary>
    /// <seealso cref="IInputListener{TInput}"/>
    [MemberNotNull(nameof(sessionServices))]
    public NetcodeSessionBuilder<TInput> WithInputListener(IInputListener<TInput> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        return ConfigureServices(s => s.InputListener = listener);
    }

    /// <summary>
    /// Set the <typeparamref name="TInput"/> comparer.
    /// </summary>
    [MemberNotNull(nameof(sessionServices))]
    public NetcodeSessionBuilder<TInput> WithComparer(IEqualityComparer<TInput> comparer)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        return ConfigureServices(s => s.InputComparer = comparer);
    }

    /// <summary>
    /// Set <see cref="INetcodeSession{TInput}"/> options
    /// </summary>
    /// <seealso cref="NetcodeOptions"/>
    public NetcodeSessionBuilder<TInput> WithOptions(NetcodeOptions options)
    {
        this.options = options;
        return this;
    }

    /// <summary>
    /// Set custom session services
    /// </summary>
    /// <seealso cref="SessionServices{TInput}"/>
    [MemberNotNull(nameof(sessionServices))]
    public NetcodeSessionBuilder<TInput> WithServices(SessionServices<TInput> services)
    {
        ArgumentNullException.ThrowIfNull(services);
        sessionServices = services;
        return this;
    }

    /// <summary>
    /// Configure custom session services
    /// </summary>
    /// <seealso cref="SessionServices{TInput}"/>
    [MemberNotNull(nameof(sessionServices))]
    public NetcodeSessionBuilder<TInput> ConfigureServices(Action<SessionServices<TInput>> config)
    {
        var services = sessionServices ?? new();
        config.Invoke(services);
        return WithServices(services);
    }

    /// <summary>
    /// Configure session logger
    /// </summary>
    /// <seealso cref="LoggerOptions"/>
    public NetcodeSessionBuilder<TInput> ConfigureLogger(Action<LoggerOptions> config)
    {
        config.Invoke(options.Logger);
        return this;
    }

    /// <summary>
    /// Configure session protocol
    /// </summary>
    /// <seealso cref="ProtocolOptions"/>
    public NetcodeSessionBuilder<TInput> ConfigureProtocol(Action<ProtocolOptions> config)
    {
        config.Invoke(options.Protocol);
        return this;
    }

    /// <summary>
    /// Configure session time synchronization
    /// </summary>
    /// <seealso cref="TimeSyncOptions"/>
    public NetcodeSessionBuilder<TInput> ConfigureTimeSync(Action<TimeSyncOptions> config)
    {
        config.Invoke(options.TimeSync);
        return this;
    }

    /// <summary>
    /// Set spectator session options.
    /// </summary>
    /// <seealso cref="SpectatorOptions"/>
    public NetcodeSessionBuilder<TInput> WithSpectatorOptions(SpectatorOptions options)
    {
        spectatorOptions = options;
        return WithMode(SessionMode.Spectator);
    }

    /// <summary>
    /// Configure spectator session options.
    /// </summary>
    /// <seealso cref="SpectatorOptions"/>
    [MemberNotNull(nameof(spectatorOptions))]
    public NetcodeSessionBuilder<TInput> ConfigureSpectator(Action<SpectatorOptions>? config = null)
    {
        spectatorOptions ??= new();
        config?.Invoke(spectatorOptions);
        return WithMode(SessionMode.Spectator);
    }

    /// <summary>
    /// Set sync test session options.
    /// </summary>
    /// <seealso cref="SyncTestOptions{TInput}"/>
    public NetcodeSessionBuilder<TInput> WithSyncTestOptions(SyncTestOptions<TInput> options)
    {
        syncTestOptions = options;
        return WithMode(SessionMode.SyncTest);
    }

    /// <summary>
    /// Configure sync test session options.
    /// </summary>
    /// <seealso cref="SyncTestOptions{TInput}"/>
    [MemberNotNull(nameof(syncTestOptions))]
    public NetcodeSessionBuilder<TInput> ConfigureSyncTest(Action<SyncTestOptions<TInput>>? config = null)
    {
        syncTestOptions ??= new();
        config?.Invoke(syncTestOptions);
        return WithMode(SessionMode.SyncTest);
    }

    /// <summary>
    /// Set replay session options.
    /// </summary>
    /// <seealso cref="SessionReplayOptions{TInput}"/>
    public NetcodeSessionBuilder<TInput> WithReplayTestOptions(SessionReplayOptions<TInput> options)
    {
        replayOptions = options;
        return WithMode(SessionMode.Replay);
    }

    /// <summary>
    /// Configure replay session options.
    /// </summary>
    /// <seealso cref="SessionReplayOptions{TInptut}"/>
    [MemberNotNull(nameof(replayOptions))]
    public NetcodeSessionBuilder<TInput> ConfigureReplay(Action<SessionReplayOptions<TInput>>? config = null)
    {
        replayOptions ??= new();
        config?.Invoke(replayOptions);
        return WithMode(SessionMode.Replay);
    }
}

/// <summary>
/// Builder for <see cref="INetcodeSession{TInput}"/>.
/// </summary>
/// <seealso cref="RollbackNetcode"/>
[SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
public sealed class NetcodeSessionBuilder
{
    /// <inheritdoc cref="InputTypeSelector"/>
    public NetcodeSessionBuilder<T> WithInputType<T>(Func<InputTypeSelector, InputTypeSelected<T>> selector)
        where T : unmanaged =>
        new(selector(new()).Serializer);

    /// <inheritdoc cref="InputTypeSelector.Enum{T}"/>
    public NetcodeSessionBuilder<T> WithInputType<T>() where T : unmanaged, Enum =>
        WithInputType(x => x.Enum<T>());

    /// <summary>
    /// Selected input type for <see name="NetcodeSessionBuilder{TInput}"/>
    /// </summary>
    public sealed class InputTypeSelected<T> where T : unmanaged
    {
        internal readonly SerializerFactory<T> Serializer;
        internal InputTypeSelected(SerializerFactory<T> serializer) => Serializer = serializer;
    }

    internal delegate IBinarySerializer<T> SerializerFactory<T>(Endianness endianness) where T : unmanaged;

    /// <summary>
    /// Selector for <see cref="INetcodeSession{TInput}"/> input type
    /// </summary>
    [Serializable]
    public sealed class InputTypeSelector
    {
        internal InputTypeSelector() { }

        /// <summary>
        /// Choose an <see cref="Enum"/> as <see cref="INetcodeSession{TInput}"/> input type
        /// </summary>
        public InputTypeSelected<T> Enum<T>() where T : unmanaged, Enum =>
            new(e => BinarySerializerFactory.ForEnum<T>(e));

        /// <summary>
        /// Choose an <see cref="IBinaryInteger{T}"/> as <see cref="INetcodeSession{TInput}"/> input type
        /// </summary>
        public InputTypeSelected<T> Integer<T>() where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T> =>
            new(e => BinarySerializerFactory.ForInteger<T>(e));

        /// <summary>
        /// Choose an <see cref="IBinaryInteger{T}"/> as <see cref="INetcodeSession{TInput}"/> input type
        /// </summary>
        public InputTypeSelected<T> Integer<T>(bool isUnsigned) where T : unmanaged, IBinaryInteger<T> =>
            new(e => IntegerBinarySerializer.Create<T>(isUnsigned, e));

        /// <summary>
        /// Choose a raw unmanaged value type as input type.
        /// Must not be a reference type or a value type that contains references.
        /// This *DO NOT* use custom <see cref="Endianness"/> for <typeparamref name="T"/> integer fields.
        /// </summary>
        /// <seealso cref="RuntimeHelpers.IsReferenceOrContainsReferences{T}"/>
        public InputTypeSelected<T> Struct<T>() where T : unmanaged =>
            new(_ => BinarySerializerFactory.ForStruct<T>());

        /// <summary>
        /// Choose a custom type and serializer for the input type.
        /// </summary>
        public InputTypeSelected<T> Custom<T>(IBinarySerializer<T> serializer) where T : unmanaged =>
            new(_ => serializer);
    }
}

#pragma warning restore S2325, CA1822
