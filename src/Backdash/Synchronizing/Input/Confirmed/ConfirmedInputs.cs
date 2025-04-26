using System.Numerics;
using System.Runtime.CompilerServices;
using Backdash.Core;

namespace Backdash.Synchronizing.Input.Confirmed;

/// <summary>
///     All confirmed inputs for all players
/// </summary>
/// <typeparam name="TInput"></typeparam>
public struct ConfirmedInputs<TInput> : IEquatable<ConfirmedInputs<TInput>> where TInput : unmanaged
{
    /// <summary>
    ///     Number of inputs
    /// </summary>
    public byte Count = InputArray<TInput>.Capacity;

    /// <summary>
    ///     Input array
    /// </summary>
    public InputArray<TInput> Inputs = new();

    /// <summary>
    ///     Initialized <see cref="ConfirmedInputs{TInput}" /> with full size
    /// </summary>
    public ConfirmedInputs() => Count = InputArray<TInput>.Capacity;

    /// <summary>
    ///     Initialized <see cref="ConfirmedInputs{TInput}" /> from span
    /// </summary>
    public ConfirmedInputs(ReadOnlySpan<TInput> inputs)
    {
        Count = (byte)inputs.Length;
        inputs.CopyTo(Inputs);
    }

    /// <summary>
    ///     Copy inputs to buffer
    /// </summary>
    public readonly void CopyTo(Span<TInput> output) => Inputs[..Count].CopyTo(output);

    /// <summary>
    ///     Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    public readonly bool Equals(in ConfirmedInputs<TInput> other)
    {
        var thisSpan = ((ReadOnlySpan<TInput>)Inputs)[..Count];
        var otherSpan = ((ReadOnlySpan<TInput>)other.Inputs)[..other.Count];
        return thisSpan.SequenceEqual(otherSpan);
    }

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is ConfirmedInputs<TInput> other && Equals(other);

    readonly bool IEquatable<ConfirmedInputs<TInput>>.Equals(ConfirmedInputs<TInput> other) =>
        Count == other.Count && Inputs.Equals(other.Inputs);

    /// <inheritdoc cref="IEqualityOperators{TSelf,TOther,TResult}.op_Inequality" />
    public static bool operator ==(in ConfirmedInputs<TInput> left, in ConfirmedInputs<TInput> right) =>
        left.Equals(in right);

    /// <inheritdoc cref="IEqualityOperators{TSelf,TOther,TResult}.op_Equality" />
    public static bool operator !=(in ConfirmedInputs<TInput> left, in ConfirmedInputs<TInput> right) =>
        !left.Equals(in right);

    /// <inheritdoc />
    public override readonly int GetHashCode() => Mem.GetHashCode(Inputs[..Count]);
}

/// <summary>
///     Array of inputs for all players
/// </summary>
/// <typeparam name="TInput"></typeparam>
[InlineArray(Capacity)]
public struct InputArray<TInput> : IEquatable<InputArray<TInput>> where TInput : unmanaged
{
    /// <summary>
    ///     Max size of <see cref="InputArray{TInput}" />
    /// </summary>
    /// <inheritdoc cref="Max.NumberOfPlayers" />
    public const int Capacity = Max.NumberOfPlayers;

#pragma warning disable S1144, IDE0051, IDE0044
    TInput element0;
#pragma warning restore IDE0051, S1144, IDE0044

    /// <summary>
    ///     Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    public readonly bool Equals(in InputArray<TInput> other) => ((ReadOnlySpan<TInput>)this).SequenceEqual(other);

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is InputArray<TInput> other && Equals(other);

    /// <inheritdoc />
    readonly bool IEquatable<InputArray<TInput>>.Equals(InputArray<TInput> other) => Equals(in other);

    /// <inheritdoc cref="IEqualityOperators{TSelf,TOther,TResult}.op_Inequality" />
    public static bool operator ==(in InputArray<TInput> a, in InputArray<TInput> b) => a.Equals(in b);

    /// <inheritdoc cref="IEqualityOperators{TSelf,TOther,TResult}.op_Equality" />
    public static bool operator !=(in InputArray<TInput> a, in InputArray<TInput> b) => !a.Equals(in b);

    /// <inheritdoc />
    public override readonly int GetHashCode() => Mem.GetHashCode<TInput>(this);
}
