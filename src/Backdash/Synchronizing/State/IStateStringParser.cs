using System.Text.Json;
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
    JsonSerializerOptions? options = null,
    IStateStringParser? stateStringFallback = null
) : IStateStringParser
{
    internal Logger? Logger = null;

    readonly JsonSerializerOptions jsonOptions = options ?? new()
    {
        WriteIndented = true,
        IncludeFields = true,
    };

    readonly IStateStringParser fallback = stateStringFallback ?? new HexStateStringParser();

    /// <inheritdoc />
    public string GetStateString(in Frame frame, ref readonly BinaryBufferReader reader, object? currentState)
    {
        if (currentState is null)
            return fallback.GetStateString(in frame, in reader, currentState);

        try
        {
            return JsonSerializer.Serialize(currentState, jsonOptions);
        }
        catch (Exception e)
        {
            Logger?.Write(LogLevel.Error, $"State Json Parser Error: {e}");
            return fallback.GetStateString(in frame, in reader, currentState);
        }
    }
}
