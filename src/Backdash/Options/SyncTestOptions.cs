using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.State;

namespace Backdash.Options;

/// <summary>
///     Configurations for <see cref="INetcodeSession{TInput}" /> in <see cref="SessionMode.SyncTest" /> mode.
/// </summary>
public sealed record SyncTestOptions<TInput> where TInput : unmanaged
{
    /// <summary>
    ///     Total forced rollback frames.
    /// </summary>
    /// <value>Defaults to <c>1</c></value>
    public int CheckDistance { get; set; } = 1;

    /// <summary>
    ///     If true, throws on state de-synchronization.
    /// </summary>
    public bool ThrowOnDesync { get; set; } = true;

    /// <summary>
    ///     Sets desync handler for <see cref="SessionMode.SyncTest" /> sessions.
    ///     Useful for showing smart state diff.
    /// </summary>
    public IStateDesyncHandler? DesyncHandler { get; set; }

    /// <summary>
    ///     Sets desync handler for <see cref="SessionMode.SyncTest" /> sessions.
    ///     Useful for showing smart state diff.
    /// </summary>
    public IStateStringParser? StateStringParser { get; set; }

    /// <summary>
    ///     Input generator service for session.
    /// </summary>
    public IInputProvider<TInput>? InputProvider { get; set; }

    /// <summary>
    ///     Use <see cref="RandomInputProvider{TInput}" /> as input provider.
    /// </summary>
    /// <seealso cref="InputProvider" />
    public SyncTestOptions<TInput> UseRandomInputProvider()
    {
        InputProvider = new RandomInputProvider<TInput>();
        return this;
    }

    /// <summary>
    ///     Use <see cref="JsonStateStringParser" /> as state viewer.
    /// </summary>
    /// <remarks>For AoT compatibility, use <see cref="UseJsonStateViewer(JsonTypeInfo)"/> instead.</remarks>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo instead.")]
    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    public SyncTestOptions<TInput> UseJsonStateViewer(JsonSerializerOptions? options = null)
    {
        StateStringParser = new JsonStateStringParser(options);
        return this;
    }

    /// <summary>
    ///     Use <see cref="JsonStateStringParser" /> as state viewer.
    /// </summary>
    public SyncTestOptions<TInput> UseJsonStateViewer(JsonTypeInfo stateTypeInfo)
    {
        StateStringParser = new JsonStateStringParser(stateTypeInfo);
        return this;
    }

    /// <inheritdoc cref="StateStringParser" />
    public SyncTestOptions<TInput> UseStateViewer<T>() where T : IStateStringParser, new()
    {
        StateStringParser = new T();
        return this;
    }

    /// <inheritdoc cref="DesyncHandler" />
    public SyncTestOptions<TInput> UseDesyncHandler<T>() where T : IStateDesyncHandler, new()
    {
        DesyncHandler = new T();
        return this;
    }

    /// <inheritdoc cref="InputProvider" />
    public SyncTestOptions<TInput> UseInputProvider<T>() where T : IInputProvider<TInput>, new()
    {
        InputProvider = new T();
        return this;
    }
}
