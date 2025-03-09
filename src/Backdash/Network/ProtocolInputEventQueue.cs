using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.Input.Confirmed;

namespace Backdash.Network;

readonly record struct GameInputEvent<TInput>(PlayerHandle Player, GameInput<TInput> Input)
    where TInput : unmanaged
{
    public readonly PlayerHandle Player = Player;
    public readonly GameInput<TInput> Input = Input;
}

interface IProtocolInputEventPublisher<TInput> where TInput : unmanaged
{
    bool Publish(in GameInputEvent<TInput> evt);
}

sealed class ProtocolInputEventQueue<TInput> : IDisposable, IProtocolInputEventPublisher<TInput>
    where TInput : unmanaged
{
    bool disposed;

    readonly Channel<GameInputEvent<TInput>> channel = Channel.CreateUnbounded<GameInputEvent<TInput>>(
        new()
        {
            SingleWriter = false,
            SingleReader = true,
            AllowSynchronousContinuations = true,
        });

    public bool TryConsume(out GameInputEvent<TInput> nextEvent) => channel.Reader.TryRead(out nextEvent);

    public bool Publish(in GameInputEvent<TInput> evt) => !disposed && channel.Writer.TryWrite(evt);

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        channel.Writer.Complete();
    }
}

sealed class ProtocolCombinedInputsEventPublisher<TInput>(IProtocolInputEventPublisher<TInput> peerInputEventPublisher)
    : IProtocolInputEventPublisher<ConfirmedInputs<TInput>>
    where TInput : unmanaged
{
    public bool Publish(in GameInputEvent<ConfirmedInputs<TInput>> evt)
    {
        var player = evt.Player;
        var frame = evt.Input.Frame;

        var span = evt.Input.Data.Inputs[..evt.Input.Data.Count];
        ref var pointer = ref MemoryMarshal.GetReference(span);
        ref var end = ref Unsafe.Add(ref pointer, span.Length);

        bool result = true;
        while (Unsafe.IsAddressLessThan(ref pointer, ref end))
        {
            result = result && peerInputEventPublisher.Publish(new(player, new(pointer, frame)));
            pointer = ref Unsafe.Add(ref pointer, 1)!;
        }

        return result;
    }
}
