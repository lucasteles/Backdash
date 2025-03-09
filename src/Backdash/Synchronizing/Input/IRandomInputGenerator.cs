using Backdash.Core;

namespace Backdash.Synchronizing.Input;

using Random = System.Random;

/// <summary>
/// Input value provider
/// </summary>
/// <typeparam name="TInput"></typeparam>
public interface IInputGenerator<out TInput> where TInput : unmanaged
{
    /// <summary>
    /// Returns the next input
    /// </summary>
    TInput Generate();
}

/// <summary>
/// Random input value provider
/// </summary>
/// <typeparam name="TInput"></typeparam>
public sealed class RandomInputGenerator<TInput> : IInputGenerator<TInput> where TInput : unmanaged
{
    Random Random { get; }

    /// <summary>
    /// Initializes new <see cref="RandomInputGenerator{TInput}"/>
    /// </summary>
    public RandomInputGenerator(Random? random = null)
    {
        ThrowIf.TypeIsReferenceOrContainsReferences<TInput>();
        Random = random ?? Random.Shared;
    }

    /// <inheritdoc />
    public TInput Generate()
    {
        TInput newInput = new();
        var buffer = Mem.AsBytes(ref newInput);
        Random.NextBytes(buffer);
        return newInput;
    }
}
