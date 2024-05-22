using System.Runtime.CompilerServices;
using Backdash.Core;

namespace Backdash.Sync.Input.Confirmed;

/// <summary>
/// All confirmed inputs for all players
/// </summary>
/// <typeparam name="TInput"></typeparam>
public record struct ConfirmedInputs<TInput> where TInput : unmanaged
{
    /// <summary>
    /// Number of inputs
    /// </summary>
    public byte Count = InputArray<TInput>.Capacity;

    /// <summary>
    /// Input array
    /// </summary>
    public InputArray<TInput> Inputs = new();

    /// <summary>
    /// Initialized <see cref="ConfirmedInputs{TInput}"/> with full size
    /// </summary>
    public ConfirmedInputs() => Count = InputArray<TInput>.Capacity;

    /// <summary>
    /// Initialized <see cref="ConfirmedInputs{TInput}"/> from span
    /// </summary>
    public ConfirmedInputs(ReadOnlySpan<TInput> inputs)
    {
        Count = (byte)inputs.Length;
        inputs.CopyTo(Inputs);
    }
}

/// <summary>
/// Array of inputs for all players
/// </summary>
/// <typeparam name="TInput"></typeparam>
[InlineArray(Capacity)]
public struct InputArray<TInput> where TInput : unmanaged
{
    /// <summary>
    /// Max size of <see cref="InputArray{TInput}"/>
    /// </summary>
    /// <inheritdoc cref="Max.NumberOfPlayers"/>
    public const int Capacity = Max.NumberOfPlayers;

    TInput element0;
}
