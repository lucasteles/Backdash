using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Backdash.Core;
using Backdash.Network;
using Backdash.Network.Protocol;
using Backdash.Serialization;
using Backdash.Serialization.Internal;
using Backdash.Synchronizing;

// ReSharper disable LocalVariableHidesMember, ParameterHidesMember
#pragma warning disable S2325, CA1822

namespace Backdash;

/// <inheritdoc cref="NetcodeSessionBuilder"/>
[SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
public sealed class NetcodeSessionBuilder<TInput> where TInput : unmanaged
{
    readonly NetcodeSessionBuilder.SerializerFactory<TInput> serializer;
    NetcodeOptions options = new();
    SessionServices<TInput>? services;
    int numberOfPlayer = 2;
    bool syncTestThrowException = true;

    internal NetcodeSessionBuilder(NetcodeSessionBuilder.SerializerFactory<TInput> serializer) =>
        this.serializer = serializer;

    /// <summary>
    /// Sets the number of players for the <see cref="INetcodeSession{TInput}"/>
    /// </summary>
    /// <value>Defaults to <c>2</c></value>
    public NetcodeSessionBuilder<TInput> WithPlayerCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        numberOfPlayer = count;

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
    public NetcodeSessionBuilder<TInput> WithServices(SessionServices<TInput> services)
    {
        this.services = services;
        return this;
    }

    /// <summary>
    /// Configure custom session services
    /// </summary>
    /// <seealso cref="SessionServices{TInput}"/>
    public NetcodeSessionBuilder<TInput> ConfigureServices(Action<SessionServices<TInput>> config)
    {
        var services = this.services ?? new();
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
}

/// <summary>
/// Builder for <see cref="INetcodeSession{TInput}"/>.
///  <seealso cref="RollbackNetcode"/>
/// </summary>
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
