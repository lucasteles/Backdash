using System.Runtime.CompilerServices;
using Backdash.Core;

namespace Backdash.Sync.Input.Spectator;

record struct CombinedInputs<TInput> where TInput : struct
{
    public byte Count = InputArray<TInput>.Capacity;
    public InputArray<TInput> Inputs = new();

    public CombinedInputs() => Count = InputArray<TInput>.Capacity;

    public CombinedInputs(ReadOnlySpan<TInput> inputs)
    {
        Count = (byte)inputs.Length;
        inputs.CopyTo(Inputs);
    }
}

[InlineArray(Capacity)]
struct InputArray<TInput> where TInput : struct
{
    public const int Capacity = Max.RemoteConnections;

    public TInput element0;
}
