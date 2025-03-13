using Backdash.Core;

namespace Backdash.Synchronizing.Input;

using Random = System.Random;

/// <summary>
///     Input value provider
/// </summary>
/// <typeparam name="TInput"></typeparam>
public interface IInputProvider<out TInput> where TInput : unmanaged
{
    /// <summary>
    ///     Returns the next input
    /// </summary>
    TInput Next();
}

/// <summary>
///     Random input value provider
/// </summary>
/// <typeparam name="TInput"></typeparam>
/// <remarks>
///     Initializes new <see cref="RandomInputProvider{TInput}" />
/// </remarks>
public sealed class RandomInputProvider<TInput>(Random? random = null)
    : IInputProvider<TInput> where TInput : unmanaged
{
    Random Random { get; } = random ?? Random.Shared;

    /// <inheritdoc />
    public TInput Next()
    {
        TInput newInput = new();
        var buffer = Mem.AsBytes(ref newInput);
        Random.NextBytes(buffer);
        return newInput;
    }
}
