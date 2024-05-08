using Backdash.Core;

namespace Backdash.Sync.Input;

/// <summary>
/// Input value provider
/// </summary>
/// <typeparam name="TInput"></typeparam>
public interface IInputGenerator<out TInput> where TInput : struct
{
    /// <summary>
    /// Returns the next input
    /// </summary>
    public TInput Generate();
}

/// <summary>
/// Random input value provider
/// </summary>
/// <typeparam name="TInput"></typeparam>
public sealed class RandomInputGenerator<TInput> : IInputGenerator<TInput> where TInput : unmanaged
{
    Random Random { get; } = Random.Shared;

    /// <summary>
    /// Initializes new <see cref="RandomInputGenerator{TInput}"/>
    /// </summary>
    public RandomInputGenerator()
    {
        ThrowHelpers.ThrowIfTypeTooBigForStack<TInput>();
        ThrowHelpers.ThrowIfTypeIsReferenceOrContainsReferences<TInput>();
    }

    /// <inheritdoc />
    public TInput Generate()
    {
        TInput newInput = new();
        var buffer = Mem.GetSpan(ref newInput);
        Random.NextBytes(buffer);
        return newInput;
    }
}
