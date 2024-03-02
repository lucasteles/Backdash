using Backdash.Core;

namespace Backdash.Sync.Input;

public interface IInputGenerator<out TInput> where TInput : struct
{
    public TInput Generate();
}

public sealed class RandomInputGenerator<TInput> : IInputGenerator<TInput> where TInput : unmanaged
{
    internal Random Random { get; set; } = Random.Shared;

    public RandomInputGenerator()
    {
        ThrowHelpers.ThrowIfTypeTooBigForStack<TInput>();
        ThrowHelpers.ThrowIfTypeIsReferenceOrContainsReferences<TInput>();
    }

    public TInput Generate()
    {
        TInput newInput = new();
        var buffer = Mem.GetSpan(ref newInput);
        Random.NextBytes(buffer);
        return newInput;
    }
}
