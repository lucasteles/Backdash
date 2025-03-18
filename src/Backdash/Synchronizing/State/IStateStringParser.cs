using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Backdash.Core;
using Backdash.Serialization;

namespace Backdash.Synchronizing.State;

/// <summary>
///     Get string representation of the state
///     Used for Sync Test logging <see cref="NetcodeSessionBuilder{TInput}.ForSyncTest" />
/// </summary>
public interface IStateStringParser
{
    /// <summary>
    ///     Parse binary state to a string representation.
    /// </summary>
    string GetStateString(in Frame frame, ref readonly BinaryBufferReader reader, object? currentState);
}

/// <inheritdoc />
sealed class HexStateStringParser : IStateStringParser
{
    /// <inheritdoc />
    public string GetStateString(in Frame frame, ref readonly BinaryBufferReader reader, object? currentState) =>
        $$"""
          {
            --- Begin Hex ---
            {{Convert.ToHexString(reader.CurrentBuffer).BreakToLines(LogStringBuffer.Capacity / 2)}}
            ---  End Hex  ---
          }
          """;
}

/// <summary>
///     Try to get the json string representation of the state.
/// </summary>
public sealed class JsonStateStringParser(
    JsonTypeInfo stateTypeInfo,
    IStateStringParser? stateStringFallback = null
) : IStateStringParser
{
    /// <summary>
    ///     Try to get the json string representation of the state.
    /// </summary>
    /// <remarks>For AoT compatibility, use <see cref="JsonStateStringParser(JsonTypeInfo,IStateStringParser?)"/> instead.</remarks>
    /// <param name="options"></param>
    /// <param name="stateStringFallback"></param>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo instead.")]
    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    public JsonStateStringParser(
    JsonSerializerOptions? options = null,
    IStateStringParser? stateStringFallback = null
    ) : this(
        (options ?? new()
        {
            WriteIndented = true,
            IncludeFields = true,
        }).GetTypeInfo(typeof(object)),
        stateStringFallback)
    {

    }

    internal Logger? Logger = null;

    readonly JsonTypeInfo typeInfo = stateTypeInfo ?? throw new ArgumentNullException(nameof(stateTypeInfo));

    readonly IStateStringParser fallback = stateStringFallback ?? new HexStateStringParser();

    /// <inheritdoc />
    public string GetStateString(in Frame frame, ref readonly BinaryBufferReader reader, object? currentState)
    {
        if (currentState is null)
            return fallback.GetStateString(in frame, in reader, currentState);

        try
        {
            return JsonSerializer.Serialize(currentState, typeInfo);
        }
        catch (Exception e)
        {
            Logger?.Write(LogLevel.Error, $"State Json Parser Error: {e}");
            return fallback.GetStateString(in frame, in reader, currentState);
        }
    }
}
